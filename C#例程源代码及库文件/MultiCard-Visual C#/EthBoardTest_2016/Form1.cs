using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EthBoardTest_2016;
using System.Runtime.InteropServices;

using MultiCardCS;
using MultiCardCLR;

namespace EthBoardTest_2016
{
    public partial class Form1 : Form
    {
        int Test = 0;
        double dDataID = 1;
        int OpenFlag = 0;

        //声明卡对象，如果有多个卡，可以声明多个对象
        MultiCardCS.MultiCardCS MultiCardCS_1 = new MultiCardCS.MultiCardCS();
        //MultiCardCS.MultiCardCS MultiCardCS_2 = new MultiCardCS.MultiCardCS();
        //MultiCardCS.MultiCardCS MultiCardCS_3 = new MultiCardCS.MultiCardCS();

        public Form1()
        {
            InitializeComponent();

            comboBoxAxisSel.Items.Add("轴1");
            comboBoxAxisSel.Items.Add("轴2");
            comboBoxAxisSel.Items.Add("轴3");
            comboBoxAxisSel.Items.Add("轴4");
            comboBoxAxisSel.Items.Add("轴5");
            comboBoxAxisSel.Items.Add("轴6");
            comboBoxAxisSel.Items.Add("轴7");
            comboBoxAxisSel.Items.Add("轴8");
            comboBoxAxisSel.Items.Add("轴9");
            comboBoxAxisSel.Items.Add("轴10");
            comboBoxAxisSel.Items.Add("轴11");
            comboBoxAxisSel.Items.Add("轴12");
            comboBoxAxisSel.Items.Add("轴13");
            comboBoxAxisSel.Items.Add("轴14");
            comboBoxAxisSel.Items.Add("轴15");
            comboBoxAxisSel.Items.Add("轴16");
            comboBoxAxisSel.SelectedIndex = 0;

            comboBox_AxisSel2.Items.Add("轴1");
            comboBox_AxisSel2.Items.Add("轴2");
            comboBox_AxisSel2.Items.Add("轴3");
            comboBox_AxisSel2.Items.Add("轴4");
            comboBox_AxisSel2.Items.Add("轴5");
            comboBox_AxisSel2.Items.Add("轴6");
            comboBox_AxisSel2.Items.Add("轴7");
            comboBox_AxisSel2.Items.Add("轴8");
            comboBox_AxisSel2.Items.Add("轴9");
            comboBox_AxisSel2.Items.Add("轴10");
            comboBox_AxisSel2.Items.Add("轴11");
            comboBox_AxisSel2.Items.Add("轴12");
            comboBox_AxisSel2.Items.Add("轴13");
            comboBox_AxisSel2.Items.Add("轴14");
            comboBox_AxisSel2.Items.Add("轴15");
            comboBox_AxisSel2.Items.Add("轴16");
            comboBox_AxisSel2.SelectedIndex = 0;

            comboBoxAxisSel3.Items.Add("轴1");
            comboBoxAxisSel3.Items.Add("轴2");
            comboBoxAxisSel3.Items.Add("轴3");
            comboBoxAxisSel3.Items.Add("轴4");
            comboBoxAxisSel3.Items.Add("轴5");
            comboBoxAxisSel3.Items.Add("轴6");
            comboBoxAxisSel3.Items.Add("轴7");
            comboBoxAxisSel3.Items.Add("轴8");
            comboBoxAxisSel3.Items.Add("轴9");
            comboBoxAxisSel3.Items.Add("轴10");
            comboBoxAxisSel3.Items.Add("轴11");
            comboBoxAxisSel3.Items.Add("轴12");
            comboBoxAxisSel3.Items.Add("轴13");
            comboBoxAxisSel3.Items.Add("轴14");
            comboBoxAxisSel3.Items.Add("轴15");
            comboBoxAxisSel3.Items.Add("轴16");
            comboBoxAxisSel3.SelectedIndex = 0;

            comboBoxCardSel.Items.Add("主卡");
            comboBoxCardSel.Items.Add("扩展卡1");
            comboBoxCardSel.Items.Add("扩展卡2");
            comboBoxCardSel.Items.Add("扩展卡3");
            comboBoxCardSel.Items.Add("扩展卡4");
            comboBoxCardSel.Items.Add("扩展卡5");
            comboBoxCardSel.Items.Add("扩展卡6");
            comboBoxCardSel.Items.Add("扩展卡7");
            comboBoxCardSel.Items.Add("扩展卡8");
            comboBoxCardSel.SelectedIndex = 0;

            comboBoxTimeUnitSel.Items.Add("微秒");
            comboBoxTimeUnitSel.Items.Add("毫秒");
            comboBoxTimeUnitSel.SelectedIndex = 0;


            textBoxRealitivePos.Text = "10000";
            textBoxAbsPos.Text = "20000";

            Timer timer1 = new Timer();
            timer1.Enabled = true;
            timer1.Interval = 100;
            timer1.Tick += new System.EventHandler(this.timer1_Tick);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            int iRes = 0;
            int lValue = 0;
            short nStatus = 0;

            if (0 == OpenFlag)
            {
                return;
            }

            short nCardIndex = (short)comboBoxCardSel.SelectedIndex;

                //8轴及以下控制卡用这个
                //MultiCardCS.MultiCardCS.TAllSysStatusData m_AllSysStatusData;
                //m_AllSysStatusData.dAxisEncPos = new double[9];
                //m_AllSysStatusData.dAxisPrfPos = new double[8];
                //m_AllSysStatusData.lAxisStatus = new int[8];
/*
//系统状态结构体
typedef struct _AllSysStatusData
{
	double dAxisEncPos[9];//轴编码器位置，包含一个手轮
	double dAxisPrfPos[8];//轴规划位置
	unsigned long lAxisStatus[8];//轴状态
	short nADCValue[2];//ADC值
	long lUserSegNum[2];//两个坐标系的用户段号
	long lRemainderSegNum[2];//两个坐标系的剩余段号
	short nCrdRunStatus[2];//两个坐标系的坐标系状态
	long lCrdSpace[2];//两个坐标系的剩余空间
	double dCrdVel[2];//两个坐标系的速度
	double dCrdPos[2][5];//两个坐标系的坐标
	long lLimitPosRaw;//正硬限位
	long lLimitNegRaw;//负硬限位
	long lAlarmRaw;//报警输入
	long lHomeRaw;//零位输入
	long lMPG;//手轮信号
	long lGpiRaw[4];//通用IO输入（除主卡外，最大支持3个扩展模块）
}TAllSysStatusData;
*/

                MultiCardCS.MultiCardCS.TAllSysStatusDataSX m_AllSysStatusData;
                m_AllSysStatusData.lAxisEncPos = new int[16];
                m_AllSysStatusData.lAxisPrfPos = new int[16];
                m_AllSysStatusData.lAxisStatus = new int[16];
                
                m_AllSysStatusData.nADCValue = new short[2];
                m_AllSysStatusData.lUserSegNum = new int[2];
                m_AllSysStatusData.lRemainderSegNum = new short[2];
                m_AllSysStatusData.nCrdRunStatus = new short[2];
                m_AllSysStatusData.lCrdSpace = new short[2];
                m_AllSysStatusData.dCrdVel = new float[2];

                m_AllSysStatusData.lCrdPos = new int[2][];
                m_AllSysStatusData.lCrdPos[0] = new int[5];
                m_AllSysStatusData.lCrdPos[1] = new int[5];

                m_AllSysStatusData.lLimitPosRaw = 0;
                m_AllSysStatusData.lLimitNegRaw = 0;
                m_AllSysStatusData.lAlarmRaw = 0;
                m_AllSysStatusData.lHomeRaw = 0;
                m_AllSysStatusData.lMPG = 0;
                m_AllSysStatusData.lGpiRaw = new int[8];
                m_AllSysStatusData.lGpoRaw = new int[8];

                m_AllSysStatusData.lMPGEncPos = 0;

                iRes = MultiCardCS_1.GA_GetAllSysStatusSX(ref m_AllSysStatusData);

                if (0 != (m_AllSysStatusData.nCrdRunStatus[0] & MultiCardCS.MultiCardCS.CRDSYS_STATUS_PROG_RUN))
                {
                    labelCrdStatsuRunning.Text = "运行状态：1";
                }
                else
                {
                    labelCrdStatsuRunning.Text = "运行状态：0";
                }

                if (0 != (m_AllSysStatusData.nCrdRunStatus[0] & MultiCardCS.MultiCardCS.CRDSYS_STATUS_FIFO_FINISH_0))
                {
                    labelFifoFinish.Text = "缓冲区数据执行完毕：1";
                }
                else
                {
                    labelFifoFinish.Text = "缓冲区数据执行完毕：0";
                }

            

                labelADC.Text = Convert.ToString(m_AllSysStatusData.nADCValue[0]);

                labelMPGEncPos.Text = Convert.ToString(m_AllSysStatusData.lMPGEncPos);

                //获取轴1的PT数据空闲空间(这里只是拿轴1做例子，每个轴都可以这样做)
                int iPTSpace = 0;
                MultiCardCS_1.GA_PtSpace(1, ref iPTSpace, 1);

                string strText = string.Format("轴1的PT空闲空间：{0:G}", iPTSpace);
                labelPTSpace.Text = strText;

                //获取轴1的PT数据剩余未完成数据(这里只是拿轴1做例子，每个轴都可以这样做)
                int iRemainSpace = 0;
                MultiCardCS_1.GA_PtRemain(1, ref iRemainSpace, 1);

                strText = string.Format("轴1的PT剩余数据：{0:G}", iRemainSpace);
                labelPTRemain.Text = strText;

                ushort nCmpSts = 0;
                ushort nRemainCount1 = 0;
                ushort nRemainCount2 = 0;
                ushort nRemainSpace1 = 0;
                ushort nRemainSpace2 = 0;
                MultiCardCS_1.GA_CmpBufSts(ref nCmpSts, ref nRemainCount1, ref nRemainCount2, ref nRemainSpace1, ref nRemainSpace2);

                strText = string.Format("待比较数据个数:{0:G}", nRemainCount1);
                labelRemainCmpDataNum.Text = strText;

                if (0 == (nCmpSts & 0x0001))
                {
                    radioButtonCMPNotEmpty.Checked = false;
                }
                else
                {
                    radioButtonCMPNotEmpty.Checked = true;
                }

                if (0 == (m_AllSysStatusData.lMPG & 0X0001))
                {
                    radioButtonMPG_X.Checked = false;
                }
                else
                {
                    radioButtonMPG_X.Checked = true;
                }

                if (0 == (m_AllSysStatusData.lMPG & 0X0002))
                {
                    radioButtonMPG_Y.Checked = false;
                }
                else
                {
                    radioButtonMPG_Y.Checked = true;
                }

                if (0 == (m_AllSysStatusData.lMPG & 0X0004))
                {
                    radioButtonMPG_Z.Checked = false;
                }
                else
                {
                    radioButtonMPG_Z.Checked = true;
                }

                if (0 == (m_AllSysStatusData.lMPG & 0X0008))
                {
                    radioButtonMPG_A.Checked = false;
                }
                else
                {
                    radioButtonMPG_A.Checked = true;
                }

                if (0 == (m_AllSysStatusData.lMPG & 0X0010))
                {
                    radioButtonMPG_B.Checked = false;
                }
                else
                {
                    radioButtonMPG_B.Checked = true;
                }

                if (0 != (m_AllSysStatusData.lMPG & 0X0020))
                {
                    radioButtonMPG_X1.Checked = true;
                    radioButtonMPG_X10.Checked = false;
                    radioButtonMPG_X100.Checked = false;
                }
                else if (0 != (m_AllSysStatusData.lMPG & 0X0040))
                {
                    radioButtonMPG_X1.Checked = false;
                    radioButtonMPG_X10.Checked = true;
                    radioButtonMPG_X100.Checked = false;
                }
                else
                {
                    radioButtonMPG_X1.Checked = false;
                    radioButtonMPG_X10.Checked = false;
                    radioButtonMPG_X100.Checked = true;
                }
                //CRD
                int lCrdSpace = 0;
                //获取坐标系剩余空间
                iRes = MultiCardCS_1.GA_CrdSpace(1, ref lCrdSpace, 0);

                if (0 == iRes)
                {
                    strText = string.Format("板卡插补缓冲区剩余空间:{0:G}", m_AllSysStatusData.lCrdSpace[0]);
                    labelRemainSegNum.Text = strText;

                    //strText = string.Format("X:{0:F3}", m_AllSysStatusData.lCrdPos[0][0]);
                    strText = string.Format("X:{0:G}", m_AllSysStatusData.lCrdPos[0][0]);
                    labelCRDPosX.Text = strText;

                    //strText = string.Format("Y:{0:F3}", m_AllSysStatusData.lCrdPos[0][1]);
                    strText = string.Format("Y:{0:G}", m_AllSysStatusData.lCrdPos[0][1]);
                    labelCRDPosY.Text = strText;

                    //strText = string.Format("Z:{0:F3}", m_AllSysStatusData.lCrdPos[0][2]);
                    strText = string.Format("Z:{0:G}", m_AllSysStatusData.lCrdPos[0][2]);
                    labelCRDPosZ.Text = strText;

                    
                    if (0 != (0X0001 & m_AllSysStatusData.nCrdRunStatus[0]))
                    {
                        radioButtonCrdRunning.Checked = true;
                    }
                    else
                    {
                        radioButtonCrdRunning.Checked = false;
                    }

                    strText = string.Format("运行行号:{0:G}", m_AllSysStatusData.lUserSegNum[0]);
                    labelUserSegNum.Text = strText;
                    
                }

                //获取坐标系位置

            /*
                double[] dPos = new double[8];

                iRes = MultiCardCS_1.GA_GetCrdPos(1, ref dPos[0]);

                strText = string.Format("X:{0:F3}", dPos[0]);
                labelCRDPosX.Text = strText;

                strText = string.Format("Y:{0:F3}", dPos[1]);
                labelCRDPosY.Text = strText;

                strText = string.Format("Z:{0:F3}", dPos[2]);
                labelCRDPosZ.Text = strText;
            */
            /*
                short nCrdRuning = 0;
                int lSegNum = 0;
                iRes = MultiCardCS_1.GA_CrdStatus(1, ref nCrdRuning, ref lSegNum,0);

                if(0 == Test)
                {
                    if (0 != nCrdRuning)
                    {
                        radioButtonCrdRunning.Checked = true;
                    }
                    else
                    {
                        radioButtonCrdRunning.Checked = false;
                    }
                }

                MultiCardCS_1.GA_GetUserSegNum(1, ref lSegNum,0);
            
                strText = string.Format("运行行号:{0:G}", lSegNum);
                labelUserSegNum.Text = strText;
               */
                short nAxisNum = (short)(comboBoxAxisSel3.SelectedIndex + 1);
                MultiCardCS_1.GA_HomeGetSts(nAxisNum, ref nStatus);

                //尚未回零
                if (0 == nStatus)
                {
                    radioButtonHaveNotHome.Checked = true;
                    radioButtonHoming.Checked = false;
                    radioButtonHomeFinish.Checked = false;
                }
                //回零中
                else if (1 == nStatus)
                {
                    radioButtonHaveNotHome.Checked = false;
                    radioButtonHoming.Checked = true;
                    radioButtonHomeFinish.Checked = false;
                }
                //回零成功
                else if (2 == nStatus)
                {
                    radioButtonHaveNotHome.Checked = false;
                    radioButtonHoming.Checked = false;
                    radioButtonHomeFinish.Checked = true;
                }

                int iAxisStatus = 0;
                int iClock = 0;

                iRes = MultiCardCS_1.GA_GetSts(nAxisNum, ref iAxisStatus, 1, ref iClock);

                //轴运行状态
                if (0 == (iAxisStatus & MultiCardCS.MultiCardCS.AXIS_STATUS_RUNNING))
                {
                    radioButtonRunning.Checked = false;
                }
                else
                {
                    radioButtonRunning.Checked = true;
                }

                //轴到位状态
                if (0 == (iAxisStatus & MultiCardCS.MultiCardCS.AXIS_STATUS_ARRIVE))
                {
                    radioButtonArrive.Checked = false;
                }
                else
                {
                    radioButtonArrive.Checked = true;
                }

                //轴正硬限位状态(注意，硬限位状态和硬限位IO是两码事)
                if (0 == (iAxisStatus & MultiCardCS.MultiCardCS.AXIS_STATUS_POS_HARD_LIMIT))
                {
                    radioButtonHardLimP.Checked = false;
                }
                else
                {
                    radioButtonHardLimP.Checked = true;
                }

                //轴负硬限位状态(注意，硬限位状态和硬限位IO是两码事)
                if (0 == (iAxisStatus & MultiCardCS.MultiCardCS.AXIS_STATUS_NEG_HARD_LIMIT))
                {
                    radioButtonHardLimN.Checked = false;
                }
                else
                {
                    radioButtonHardLimN.Checked = true;
                }

                //轴正软限位状态
                if (0 == (iAxisStatus & MultiCardCS.MultiCardCS.AXIS_STATUS_POS_SOFT_LIMIT))
                {
                    radioButtonSoftLimP.Checked = false;
                }
                else
                {
                    radioButtonSoftLimP.Checked = true;
                }

                //轴负软限位状态
                if (0 == (iAxisStatus & MultiCardCS.MultiCardCS.AXIS_STATUS_NEG_SOFT_LIMIT))
                {
                    radioButtonSoftLimN.Checked = false;
                }
                else
                {
                    radioButtonSoftLimN.Checked = true;
                }
            
//--------------------------------------------------------------------------------------------

                iRes = MultiCardCS_1.GA_GetExtDiValue(nCardIndex, ref lValue, 1);

            //-----------
            if (0 == (lValue & 0X0001))
            {
                radioButton1.Checked = false;
            }
            else
            {
                radioButton1.Checked = true;
            }

            //-----------
            if (0 == (lValue & 0X0002))
            {
                radioButton2.Checked = false;
            }
            else
            {
                radioButton2.Checked = true;
            }

            //-----------
            if (0 == (lValue & 0X0004))
            {
                radioButton3.Checked = false;
            }
            else
            {
                radioButton3.Checked = true;
            }

            //-----------
            if (0 == (lValue & 0X0008))
            {
                radioButton4.Checked = false;
            }
            else
            {
                radioButton4.Checked = true;
            }

            //-----------
            if (0 == (lValue & 0X0010))
            {
                radioButton5.Checked = false;
            }
            else
            {
                radioButton5.Checked = true;
            }

            //-----------
            if (0 == (lValue & 0X0020))
            {
                radioButton6.Checked = false;
            }
            else
            {
                radioButton6.Checked = true;
            }

            //-----------
            if (0 == (lValue & 0X0040))
            {
                radioButton7.Checked = false;
            }
            else
            {
                radioButton7.Checked = true;
            }

            //-----------
            if (0 == (lValue & 0X0080))
            {
                radioButton8.Checked = false;
            }
            else
            {
                radioButton8.Checked = true;
            }

            //-----------
            if (0 == (lValue & 0X0100))
            {
                radioButton9.Checked = false;
            }
            else
            {
                radioButton9.Checked = true;
            }

            //-----------
            if (0 == (lValue & 0X0200))
            {
                radioButton10.Checked = false;
            }
            else
            {
                radioButton10.Checked = true;
            }

            //-----------
            if (0 == (lValue & 0X0400))
            {
                radioButton11.Checked = false;
            }
            else
            {
                radioButton11.Checked = true;
            }

            //-----------
            if (0 == (lValue & 0X0800))
            {
                radioButton12.Checked = false;
            }
            else
            {
                radioButton12.Checked = true;
            }

            //-----------
            if (0 == (lValue & 0X1000))
            {
                radioButton13.Checked = false;
            }
            else
            {
                radioButton13.Checked = true;
            }

            //-----------
            if (0 == (lValue & 0X2000))
            {
                radioButton14.Checked = false;
            }
            else
            {
                radioButton14.Checked = true;
            }

            //-----------
            if (0 == (lValue & 0X4000))
            {
                radioButton15.Checked = false;
            }
            else
            {
                radioButton15.Checked = true;
            }

            //-----------
            if (0 == (lValue & 0X8000))
            {
                radioButton16.Checked = false;
            }
            else
            {
                radioButton16.Checked = true;
            }
//-----------------------------------------------------------------------------
            iRes = MultiCardCS_1.GA_GetDiRaw(MultiCardCS.MultiCardCS.MC_LIMIT_NEGATIVE, ref lValue);
            //-----------
            if (0 == (lValue & 0X0001))
            {
                radioButton17.Checked = false;
            }
            else
            {
                radioButton17.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0002))
            {
                radioButton18.Checked = false;
            }
            else
            {
                radioButton18.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0004))
            {
                radioButton19.Checked = false;
            }
            else
            {
                radioButton19.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0008))
            {
                radioButton20.Checked = false;
            }
            else
            {
                radioButton20.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0010))
            {
                radioButton21.Checked = false;
            }
            else
            {
                radioButton21.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0020))
            {
                radioButton22.Checked = false;
            }
            else
            {
                radioButton22.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0040))
            {
                radioButton23.Checked = false;
            }
            else
            {
                radioButton23.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0080))
            {
                radioButton24.Checked = false;
            }
            else
            {
                radioButton24.Checked = true;
            }
//----------------------------------------------------------------
            iRes = MultiCardCS_1.GA_GetDiRaw(MultiCardCS.MultiCardCS.MC_LIMIT_POSITIVE, ref lValue);
            //-----------
            if (0 == (lValue & 0X0001))
            {
                radioButton32.Checked = false;
            }
            else
            {
                radioButton32.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0002))
            {
                radioButton31.Checked = false;
            }
            else
            {
                radioButton31.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0004))
            {
                radioButton30.Checked = false;
            }
            else
            {
                radioButton30.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0008))
            {
                radioButton29.Checked = false;
            }
            else
            {
                radioButton29.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0010))
            {
                radioButton28.Checked = false;
            }
            else
            {
                radioButton28.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0020))
            {
                radioButton27.Checked = false;
            }
            else
            {
                radioButton27.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0040))
            {
                radioButton26.Checked = false;
            }
            else
            {
                radioButton26.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0080))
            {
                radioButton25.Checked = false;
            }
            else
            {
                radioButton25.Checked = true;
            }
//----------------------------------------------------------------
            iRes = MultiCardCS_1.GA_GetDiRaw(MultiCardCS.MultiCardCS.MC_HOME, ref lValue);
            //-----------
            if (0 == (lValue & 0X0001))
            {
                radioButton40.Checked = false;
            }
            else
            {
                radioButton40.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0002))
            {
                radioButton39.Checked = false;
            }
            else
            {
                radioButton39.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0004))
            {
                radioButton38.Checked = false;
            }
            else
            {
                radioButton38.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0008))
            {
                radioButton37.Checked = false;
            }
            else
            {
                radioButton37.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0010))
            {
                radioButton36.Checked = false;
            }
            else
            {
                radioButton36.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0020))
            {
                radioButton35.Checked = false;
            }
            else
            {
                radioButton35.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0040))
            {
                radioButton34.Checked = false;
            }
            else
            {
                radioButton34.Checked = true;
            }
            //-----------
            if (0 == (lValue & 0X0080))
            {
                radioButton33.Checked = false;
            }
            else
            {
                radioButton33.Checked = true;
            }
//----------------------------------------------
            double dAxisPos = 0;
            int lClock = 0;
            MultiCardCS_1.GA_GetPrfPos((short)(comboBoxAxisSel3.SelectedIndex + 1), ref dAxisPos, 1, ref lClock);
            labelPrfPos.Text = Convert.ToString(dAxisPos);

//----------------------------------------------
            MultiCardCS_1.GA_GetAxisEncPos((short)(comboBoxAxisSel3.SelectedIndex + 1), ref dAxisPos, 1, ref lClock);
            labelEncPos.Text = Convert.ToString(dAxisPos);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int iRes = 0;

            //GA_Open的4个参数依次是卡号、PC端IP地址、PC端端口号、板卡端IP地址、板卡端端口号
            //如注释部分，同时打开3个板卡代码如下
            //注意板卡端端口号必须和PC端端口号保持一致
            iRes = MultiCardCS_1.GA_Open(1, "192.168.0.200",60000,"192.168.0.1",60000);
            //iRes = MultiCardCS_2.GA_Open(2, "192.168.0.200", 60001, "192.168.0.2", 60001);
            //iRes = MultiCardCS_3.GA_Open(3, "192.168.0.200", 60002, "192.168.0.3", 60002);

            if (iRes == 0)
            {
                MessageBox.Show("打开板卡成功！");

                OpenFlag = 1;
            }
            else
            {
                MessageBox.Show("打开板卡失败！");
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            int iRes = 0;

            iRes = MultiCardCS_1.GA_Reset();
            if (iRes == 0)
            {
                MessageBox.Show("复位板卡成功！");
            }
            else
            {
                MessageBox.Show("复位板卡失败！");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            int iRes = 0;

            iRes = MultiCardCS_1.GA_Close();
            if (iRes == 0)
            {
                MessageBox.Show("关闭板卡成功！");
            }
            else
            {
                MessageBox.Show("关闭板卡失败！");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            short nBitIndex = 0;
            short nValue = 0;

            short nCardIndex = (short)comboBoxCardSel.SelectedIndex;

            iRes = MultiCardCS_1.GA_GetExtDoBit(nCardIndex, nBitIndex, ref nValue);

            if (0 == nValue)
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 1);
            }
            else
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 0);
            }
            
        }

        private void button5_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            short nBitIndex = 1;
            short nValue = 0;

            short nCardIndex = (short)comboBoxCardSel.SelectedIndex;

            iRes = MultiCardCS_1.GA_GetExtDoBit(nCardIndex, nBitIndex, ref nValue);

            if (0 == nValue)
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 1);
            }
            else
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 0);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            short nBitIndex = 2;
            short nValue = 0;

            short nCardIndex = (short)comboBoxCardSel.SelectedIndex;

            iRes = MultiCardCS_1.GA_GetExtDoBit(nCardIndex, nBitIndex, ref nValue);

            if (0 == nValue)
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 1);
            }
            else
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 0);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            short nBitIndex = 3;
            short nValue = 0;

            short nCardIndex = (short)comboBoxCardSel.SelectedIndex;

            iRes = MultiCardCS_1.GA_GetExtDoBit(nCardIndex, nBitIndex, ref nValue);

            if (0 == nValue)
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 1);
            }
            else
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 0);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            short nBitIndex = 4;
            short nValue = 0;

            short nCardIndex = (short)comboBoxCardSel.SelectedIndex;

            iRes = MultiCardCS_1.GA_GetExtDoBit(nCardIndex, nBitIndex, ref nValue);

            if (0 == nValue)
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 1);
            }
            else
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 0);
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            short nBitIndex = 5;
            short nValue = 0;

            short nCardIndex = (short)comboBoxCardSel.SelectedIndex;

            iRes = MultiCardCS_1.GA_GetExtDoBit(nCardIndex, nBitIndex, ref nValue);

            if (0 == nValue)
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 1);
            }
            else
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 0);
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            short nBitIndex = 6;
            short nValue = 0;

            short nCardIndex = (short)comboBoxCardSel.SelectedIndex;

            iRes = MultiCardCS_1.GA_GetExtDoBit(nCardIndex, nBitIndex, ref nValue);

            if (0 == nValue)
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 1);
            }
            else
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 0);
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            short nBitIndex = 7;
            short nValue = 0;

            short nCardIndex = (short)comboBoxCardSel.SelectedIndex;

            iRes = MultiCardCS_1.GA_GetExtDoBit(nCardIndex, nBitIndex, ref nValue);

            if (0 == nValue)
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 1);
            }
            else
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 0);
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            short nBitIndex = 8;
            short nValue = 0;

            short nCardIndex = (short)comboBoxCardSel.SelectedIndex;

            iRes = MultiCardCS_1.GA_GetExtDoBit(nCardIndex, nBitIndex, ref nValue);

            if (0 == nValue)
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 1);
            }
            else
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 0);
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            short nBitIndex = 9;
            short nValue = 0;

            short nCardIndex = (short)comboBoxCardSel.SelectedIndex;

            iRes = MultiCardCS_1.GA_GetExtDoBit(nCardIndex, nBitIndex, ref nValue);

            if (0 == nValue)
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 1);
            }
            else
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 0);
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            short nBitIndex = 10;
            short nValue = 0;

            short nCardIndex = (short)comboBoxCardSel.SelectedIndex;

            iRes = MultiCardCS_1.GA_GetExtDoBit(nCardIndex, nBitIndex, ref nValue);

            if (0 == nValue)
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 1);
            }
            else
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 0);
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            short nBitIndex = 11;
            short nValue = 0;

            short nCardIndex = (short)comboBoxCardSel.SelectedIndex;

            iRes = MultiCardCS_1.GA_GetExtDoBit(nCardIndex, nBitIndex, ref nValue);

            if (0 == nValue)
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 1);
            }
            else
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 0);
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            short nBitIndex = 12;
            short nValue = 0;

            short nCardIndex = (short)comboBoxCardSel.SelectedIndex;

            iRes = MultiCardCS_1.GA_GetExtDoBit(nCardIndex, nBitIndex, ref nValue);

            if (0 == nValue)
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 1);
            }
            else
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 0);
            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            short nBitIndex = 13;
            short nValue = 0;

            short nCardIndex = (short)comboBoxCardSel.SelectedIndex;

            iRes = MultiCardCS_1.GA_GetExtDoBit(nCardIndex, nBitIndex, ref nValue);

            if (0 == nValue)
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 1);
            }
            else
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 0);
            }
        }

        private void button18_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            short nBitIndex = 14;
            short nValue = 0;

            short nCardIndex = (short)comboBoxCardSel.SelectedIndex;

            iRes = MultiCardCS_1.GA_GetExtDoBit(nCardIndex, nBitIndex, ref nValue);

            if (0 == nValue)
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 1);
            }
            else
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 0);
            }
        }

        private void button19_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            short nBitIndex = 15;
            short nValue = 0;

            short nCardIndex = (short)comboBoxCardSel.SelectedIndex;

            iRes = MultiCardCS_1.GA_GetExtDoBit(nCardIndex, nBitIndex, ref nValue);

            if (0 == nValue)
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 1);
            }
            else
            {
                iRes = MultiCardCS_1.GA_SetExtDoBit(nCardIndex, nBitIndex, 0);
            }
        }

        private void button22_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            int lValue = 0XFFFF;
            iRes = MultiCardCS_1.GA_SetExtDoValue(0, ref lValue, 1);
        }

        private void button23_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            int lValue = 0;
            iRes = MultiCardCS_1.GA_SetExtDoValue(0, ref lValue, 1);
        }

        private void button20_Click(object sender, EventArgs e)
        {
            //响应函数在button20_MouseDown
        }

        private void button21_Click(object sender, EventArgs e)
        {
            //响应函数在button21_MouseDown
        }

        private void button20_MouseDown(object sender, MouseEventArgs e)
        {
            short nAxisNum = (short)(comboBoxAxisSel.SelectedIndex + 1);
            int iRes = 0;
            MultiCardCS.MultiCardCS.TJogPrm m_JogPrm;

            //加速度，单位：脉冲/毫秒/毫秒
            m_JogPrm.dAcc = 1;
            //减速度，单位：脉冲/毫秒/毫秒
            m_JogPrm.dDec = 1;
            //平滑时间(需要设置为0)
            m_JogPrm.dSmooth = 0;

            //使能轴（通常设置一次即可，不是每次必须）
            iRes = MultiCardCS_1.GA_AxisOn(nAxisNum);
            //设置为速度模式（通常设置一次即可，不是每次必须）
            iRes = MultiCardCS_1.GA_PrfJog(nAxisNum);

            //设置运动参数
            iRes = MultiCardCS_1.GA_SetJogPrm(nAxisNum, ref m_JogPrm);

            //设置速度
            iRes = MultiCardCS_1.GA_SetVel(nAxisNum, 20);

            //启动运动
            iRes = MultiCardCS_1.GA_Update(0X0001 << (nAxisNum-1));
        }

        private void button20_MouseUp(object sender, MouseEventArgs e)
        {
            //停止所有轴和坐标系
            MultiCardCS_1.GA_Stop(0XFFFFF, 0XFFFFF);
        }

        private void button21_MouseDown(object sender, MouseEventArgs e)
        {
            short nAxisNum = (short)(comboBoxAxisSel.SelectedIndex + 1);
            int iRes = 0;
            MultiCardCS.MultiCardCS.TJogPrm m_JogPrm;

            //加速度，单位：脉冲/毫秒/毫秒
            m_JogPrm.dAcc = 0.25;
            //减速度，单位：脉冲/毫秒/毫秒
            m_JogPrm.dDec = 0.25;
            //平滑时间(需要设置为0)
            m_JogPrm.dSmooth = 0;

            //使能轴（通常设置一次即可，不是每次必须）
            iRes = MultiCardCS_1.GA_AxisOn(nAxisNum);

            iRes = MultiCardCS_1.GA_ClrSts(nAxisNum,1);
            //设置为速度模式（通常设置一次即可，不是每次必须）
            iRes = MultiCardCS_1.GA_PrfJog(nAxisNum);

            //设置运动参数
            iRes = MultiCardCS_1.GA_SetJogPrm(nAxisNum, ref m_JogPrm);

            //设置速度
            iRes = MultiCardCS_1.GA_SetVel(nAxisNum, -20);

            //启动运动
            iRes = MultiCardCS_1.GA_Update(0X0001 << (nAxisNum - 1));
        }

        private void button21_MouseUp(object sender, MouseEventArgs e)
        {
            //停止所有轴和坐标系
            MultiCardCS_1.GA_Stop(0XFFFF, 0XFFFF);
        }

        private void button25_Click(object sender, EventArgs e)
        {
            double dPrfPos = 0;
            int lClock = 0;
            
            //short nAxisNum = (short)(comboBoxAxisSel.SelectedIndex + 1);
            short nAxisNum = (short)(comboBox_AxisSel2.SelectedIndex + 1);

            int iRes = 0;
            MultiCardCS.MultiCardCS.TTrapPrm m_TrapPrm;

            //加速度，单位：脉冲/毫秒/毫秒
            m_TrapPrm.acc = 1;
            //减速度，单位：脉冲/毫秒/毫秒
            m_TrapPrm.dec = 1;
            //平滑时间(需要设置为0)
            m_TrapPrm.smoothTime = 0;
            //启动速度(需要设置为0)
            m_TrapPrm.velStart = 0;

            //使能轴（通常设置一次即可，不是每次必须）
            iRes = MultiCardCS_1.GA_AxisOn(nAxisNum);
            //设置为点位模式（通常设置一次即可，不是每次必须）
            iRes = MultiCardCS_1.GA_PrfTrap(nAxisNum);

            //设置点位运动参数
            iRes = MultiCardCS_1.GA_SetTrapPrm(nAxisNum, ref m_TrapPrm);
            //设置点位运动速度
            iRes = MultiCardCS_1.GA_SetVel(nAxisNum, 20);

            //获取当前规划位置（脉冲位置）
            iRes = MultiCardCS_1.GA_GetPrfPos(nAxisNum, ref dPrfPos, 1, ref lClock);

            //设置目标位置
            iRes = MultiCardCS_1.GA_SetPos(nAxisNum,(int)(dPrfPos + int.Parse(textBoxRealitivePos.Text)));

            //启动运动
            iRes = MultiCardCS_1.GA_Update(0X0001 << (nAxisNum - 1));
        }

        private void button26_Click(object sender, EventArgs e)
        {

        }

        private void button27_Click(object sender, EventArgs e)
        {
            short nAxisNum = (short)(comboBoxAxisSel.SelectedIndex + 1);
            int iRes = 0;
            MultiCardCS.MultiCardCS.TTrapPrm m_TrapPrm;

            //加速度，单位：脉冲/毫秒/毫秒
            m_TrapPrm.acc = 1;
            //减速度，单位：脉冲/毫秒/毫秒
            m_TrapPrm.dec = 1;
            //平滑时间(需要设置为0)
            m_TrapPrm.smoothTime = 0;
            //启动速度(需要设置为0)
            m_TrapPrm.velStart = 0;

            //使能轴（通常设置一次即可，不是每次必须）
            iRes = MultiCardCS_1.GA_AxisOn(nAxisNum);
            //设置为点位模式（通常设置一次即可，不是每次必须）
            iRes = MultiCardCS_1.GA_PrfTrap(nAxisNum);

            //设置点位运动参数
            iRes = MultiCardCS_1.GA_SetTrapPrm(nAxisNum, ref m_TrapPrm);
            //设置点位运动速度
            iRes = MultiCardCS_1.GA_SetVel(nAxisNum, 20);

            //设置目标位置
            iRes = MultiCardCS_1.GA_SetPos(nAxisNum, (int)(int.Parse(textBoxAbsPos.Text)));

            //启动运动
            iRes = MultiCardCS_1.GA_Update(0X0001 << (nAxisNum - 1));
        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void button24_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            //int count1 = 0; 
            //int count2 = 0;
            sbyte[] nData = new sbyte[256];
            //short nLength = 0;

            short nAxisNum = (short)(comboBoxAxisSel3.SelectedIndex + 1);
            iRes = MultiCardCS_1.GA_ZeroPos(nAxisNum,1);

        }

        private void button_SetCrd_Click(object sender, EventArgs e)
        {
            int iRes = 0;

            //---------------------------------------建立坐标系------------------------------------------------

            MultiCardCS_1.GA_Stop(0x0001 << 16, 0x0001 << 16);

            //坐标系参数初始化
            MultiCardCS.MultiCardCS.TCrdPrm crdPrm;

            //设置坐标系最小匀速时间为0
            crdPrm.evenTime = 0;

            //设置坐标系维数为3
            crdPrm.dimension = 3;

            //设置坐标系规划对应轴通道，X对应轴1，Y对应轴2，Z对应轴3
            crdPrm.profile = new short[32];
            crdPrm.profile[0] = 1;
            crdPrm.profile[1] = 2;
            crdPrm.profile[2] = 3;

            //设置坐标系最大加速度为0.2脉冲/毫秒^2
            crdPrm.synAccMax = 1;
            //设置坐标系最大速度为100脉冲/毫秒
            crdPrm.synVelMax = 1000;

            //设置坐标系原点
            //设置原点坐标值标志,0:默认当前规划位置为原点位置;1:用户指定原点位置
            crdPrm.setOriginFlag = 1;

            crdPrm.originPos = new int[32];
            crdPrm.originPos[0] = int.Parse(textBoxOrgX.Text);
            crdPrm.originPos[1] = int.Parse(textBoxOrgY.Text);
            crdPrm.originPos[2] = int.Parse(textBoxOrgZ.Text); 

            //设置坐标系参数，建立坐标系
            iRes = MultiCardCS_1.GA_SetCrdPrm(1, ref crdPrm);

            if (0 == iRes)
            {
                MessageBox.Show("建立坐标系成功！");
            }
            else
            {
                MessageBox.Show("建立坐标系失败！");
                //return;
            }
            //------------------------------------------------------------------------------------------------


            //---------------------------------------初始化前瞻缓冲区-----------------------------------------
            //声明前瞻缓冲区参数
            MultiCardCS.MultiCardCS.TLookAheadPrm LookAheadPrm;

            //各轴的最大加速度，单位脉冲/毫秒^2

            LookAheadPrm.dAccMax = new double[MultiCardCS.MultiCardCS.INTERPOLATION_AXIS_MAX];
            LookAheadPrm.dAccMax[0] = 3;
            LookAheadPrm.dAccMax[1] = 3;
            LookAheadPrm.dAccMax[2] = 3;

            //各轴的最大速度变化量（相当于启动速度）,单位脉冲/毫秒
            LookAheadPrm.dMaxStepSpeed = new double[MultiCardCS.MultiCardCS.INTERPOLATION_AXIS_MAX];
            LookAheadPrm.dMaxStepSpeed[0] = 10;
            LookAheadPrm.dMaxStepSpeed[1] = 10;
            LookAheadPrm.dMaxStepSpeed[2] = 10;

            //各轴的脉冲当量(通常为1)
            LookAheadPrm.dScale = new double[MultiCardCS.MultiCardCS.INTERPOLATION_AXIS_MAX];
            LookAheadPrm.dScale[0] = 1;
            LookAheadPrm.dScale[1] = 1;
            LookAheadPrm.dScale[2] = 1;

            //各轴的最大速度(脉冲/毫秒)
            LookAheadPrm.dSpeedMax = new double[MultiCardCS.MultiCardCS.INTERPOLATION_AXIS_MAX];
            LookAheadPrm.dSpeedMax[0] = 1000;
            LookAheadPrm.dSpeedMax[1] = 1000;
            LookAheadPrm.dSpeedMax[2] = 1000;
            LookAheadPrm.dSpeedMax[3] = 1000;
            LookAheadPrm.dSpeedMax[4] = 1000;

            //定义前瞻缓冲区长度
            LookAheadPrm.lookAheadNum = MultiCardCS.MultiCardCS.FROCAST_LEN;

            LookAheadPrm.pLookAheadBuf = 0;

            //初始化前瞻缓冲区
            iRes = MultiCardCS_1.GA_InitLookAhead(1, 0, ref LookAheadPrm);
            //Console.WriteLine(LookAheadBuf);

            if (0 == iRes)
            {
                MessageBox.Show("初始化前瞻缓冲区成功！");
            }
            else
            {
                MessageBox.Show("初始化前瞻缓冲区失败！");
                return;
            }
            
        }

        private void button_InsertData_Click(object sender, EventArgs e)
        {
            int iRes = 0;

            float[] dAcc = new float[16];
            float[] dVel = new float[16];

            int[] lPosTemp = new int[16];

            MultiCardCS_1.GA_EncOff(1);
            MultiCardCS_1.GA_EncOff(2);
            MultiCardCS_1.GA_EncOff(3);
            MultiCardCS_1.GA_EncOff(4);
            MultiCardCS_1.GA_EncOff(5);

            for(int i=0; i<16; i++)
            {
                dAcc[i] = 1;
            }

            for(int i=0; i<16; i++)
            {
                dVel[i] = 50;
            }

            int[] lPos = new int[16];

            /*
            for (int i = 0; i < 1; i++)
            {
                lPos[0] = 50000;
                MultiCardCS_1.GA_LnAll(1, ref lPos[0], 10, 50, 1, 0, 0, 0);

                lPos[0] = 0;
                MultiCardCS_1.GA_LnAll(1, ref lPos[0], 10, 50, 1, 0, 0, 0);
            }

            MultiCardCS_1.GA_BufIOEx(1, MultiCardCS.MultiCardCS.MC_GPO, 0, 0X00030000, 0X00000000, 0, 0);
            */

            
            for (int i = 0; i < 10; i++)
            {
                iRes = MultiCardCS_1.GA_LnXYZ(1, 10000, 0, 0, 50, 1, 0, 0, 0);
                //MultiCardCS_1.GA_BufDelay(1, 1000, 0, 0);
                iRes = MultiCardCS_1.GA_LnXYZ(1, 0, 0, 0, 50, 1, 0, 0, 1);
            }

            for (int i = 0; i < 1; i++)
            {
                iRes = MultiCardCS_1.GA_LnXYZ(1, 0, 0, 10000, 50, 1, 0, 0, 0);
               // MultiCardCS_1.GA_BufDelay(1, 1000, 0, 0);
                iRes = MultiCardCS_1.GA_LnXYZ(1, 0, 0, 0, 50, 1, 0, 0, 1);
            }
            

            iRes += MultiCardCS_1.GA_CrdData(1, 0, 0);
            /*
            MultiCardCS_1.GA_BufMoveAcc(1, 0X0004, ref dAcc[0], 0, 0);
            MultiCardCS_1.GA_BufMoveVel(1, 0X0004, ref dVel[0], 0, 0);
            for (int i = 0; i < 50; i++)
            {
                lPosTemp[2] = 20000;
                MultiCardCS_1.GA_BufMove(1, 0X0004, ref lPosTemp[0],0X0004,0,0);
                MultiCardCS_1.GA_BufDelay(1, 50, 0, 0);

                lPosTemp[2] = 0;
                MultiCardCS_1.GA_BufMove(1, 0X0004, ref lPosTemp[0], 0X0004, 0, 0);
                MultiCardCS_1.GA_BufDelay(1, 50, 0, 0);
            }
             * */
                /*
                unchecked
                {
                    short a = (short)0X0010;

                    //设置速度
                    dVel[4] = 200;
                    MultiCardCS_1.GA_BufMoveVel(1, a, ref dVel[0], 0, 0);

                    //设置加速度
                    dAcc[4] = 1;
                    MultiCardCS_1.GA_BufMoveAcc(1, a, ref dAcc[0], 0, 0);

                    dAcc[4] = 0.001F;
                    MultiCardCS_1.GA_BufMoveDec(1, a, ref dAcc[0], 0, 0);
                }
                for (int i = 0; i < 10; i++)
                {
                    unchecked
                    {
                        short AxisMask = (short)0X0010;

                        lPosTemp[4] = 50000;
                        MultiCardCS_1.GA_BufMove(1, AxisMask, ref lPosTemp[0], AxisMask, 0, 0);

                        lPosTemp[4] = 0;
                        MultiCardCS_1.GA_BufMove(1, AxisMask, ref lPosTemp[0], AxisMask, 0, 0);
                    
                    }
                }
             */
            /*
                for (int i = 0; i < 1; i++)
                {
                    iRes = MultiCardCS_1.GA_LnXYZAB(1, 10000, 0, 0, 10000, 10000, 50, 1, 50, 0, 0);
                    iRes = MultiCardCS_1.GA_LnXYZAB(1, 0, 0, 0, 0, 0, 50, 1, 50, 0, 0);
                    iRes = MultiCardCS_1.GA_LnXYZAB(1, 10000, 0, 0, 10000, 10000, 50, 1, 50, 0, 0);
                    iRes = MultiCardCS_1.GA_LnXYZAB(1, 0, 0, 0, 0, 0, 50, 1, 50, 0, 0);
                    iRes = MultiCardCS_1.GA_LnXYZAB(1, 10000, 0, 0, 10000, 10000, 50, 1, 0, 0, 0);
                    iRes = MultiCardCS_1.GA_LnXYZAB(1, 0, 0, 0, 0, 0, 50, 1, 0, 0, 1);
                }
             */

            //MultiCardCS_1.GA_BufMoveAccEX(1, 0X8000, ref dVel[0], 0, 0);
            /*
            int []pPos = new int[8];
            pPos[1] = 200;
            iRes = MultiCardCS_1.GA_BufMove(1, 0X0002, ref pPos[0], 0X0002, 0, 1);
            iRes = MultiCardCS_1.GA_CrdData(1, 0, 0);
            return;
            

            //清空插补缓冲区原有数据
            //iRes = MultiCardCS_1.GA_CrdClear(1, 0);

            int R = 200;
            int iScale = 1000;

            //注意，这里仅仅是个简单示范，如果插补段数据很多，可以一边插补一边压入插补数据，不需要一次压入所有数据才启动

            //移动到起点
            MultiCardCS_1.GA_StartDebugLog();

            for (int i = 0; i < 10; i++)
            {
                iRes = MultiCardCS_1.GA_LnXYZ(1, 10000, 0, 0, 50, 1, 0, 0, 0);
                iRes = MultiCardCS_1.GA_LnXYZ(1, 0, 0, 0, 50, 1, 0, 0, 1);
            }
             */

            //double dVel = 50;
            //插入二维直线数据，X=10000,Y=20000,速度=50脉冲/ms，加速度=0.2脉冲/毫秒^2,终点速度为0，Fifo为0，用户自定义行号为1
            /*
             * iRes = MultiCardCS_1.GA_LnXYZ(1, 10000, 20000, 0, 50, 0.2, 0, 0, 1);
            iRes = MultiCardCS_1.GA_LnXYZ(1, 0, 0, 0, 50, 0.2, 0, 0, 1);
            iRes = MultiCardCS_1.GA_LnXYZ(1, 50000, 0, 0, 50, 0.2, 0, 0, 2);
             * */

            /*
            //插入三维直线数据，X=0,Y=0,Z=0速度=50脉冲/ms，加速度=0.2脉冲/毫秒^2,终点速度为0，Fifo为0，用户自定义行号为2
            iRes = GA_LnXYZ(1,0,0,0,50,0.2,0,0,2);

            //插入三维直线数据，X=10000,Y=20000,Z=2000,速度=50脉冲/ms，加速度=0.2脉冲/毫秒^2,终点速度为0，Fifo为0，用户自定义行号为3
            iRes += GA_LnXYZ(1,10000,20000,2000,50,0.2,0,0,3);

            //插入三维直线数据，X=0,Y=0,Z=0,速度=50脉冲/ms，加速度=0.2脉冲/毫秒^2,终点速度为0，Fifo为0，用户自定义行号为4
            iRes += GA_LnXYZ(1,0,0,0,50,0.2,0,0,4);

            //插入二维直线数据，X=10000,Y=20000,速度=50脉冲/ms，加速度=0.2脉冲/毫秒^2,终点速度为0，Fifo为0，用户自定义行号为5
            iRes = GA_LnXY(1,10000,20000,50,0.2,0,0,5);

            //插入IO输出指令（不会影响运动）
            iRes = GA_BufIO(1,MC_GPO,0,0X0001,0X0001,0,5);

            //插入二维直线数据，X=10000,Y=20000,速度=50脉冲/ms，加速度=0.2脉冲/毫秒^2,终点速度为0，Fifo为0，用户自定义行号为1
            iRes = GA_LnXY(1,50000,60000,50,0.2,0,0,6);

            //插入IO输出指令（不会影响运动）
            iRes = GA_BufIO(1,MC_GPO,0,0X0001,0X0000,0,6);

            //插入三维直线数据，X=0,Y=0,Z=0,速度=50脉冲/ms，加速度=0.2脉冲/毫秒^2,终点速度为0，Fifo为0，用户自定义行号为7
            iRes += GA_LnXYZ(1,0,0,0,50,0.2,0,0,7);
            */
            //iRes += GA_ArcXYC(1,50000,50000,25000,0,0,50,0.2,0,0,8);

            //iRes = GA_BufDelay(1,2000,0,8);

            //iRes += MultiCardCS_1.GA_ArcXYC(1, 50000, 0, 0, -25000, 1, 50, 0.2, 0, 0, 3);

            //iRes = GA_BufDelay(1,2000,0,8);

            //iRes = MultiCardCS_1.GA_LnXYZ(1, 0, 0, 0, 50, 0.2, 0, 0, 4);

            //插入最后一行标识符（系统会把前瞻缓冲区数据全部压入板卡）
            iRes += MultiCardCS_1.GA_CrdData(1, 0, 0);

            if (0 == iRes)
            {
                //MessageBox.Show("插入插补数据成功！");
            }
            else
            {
                MessageBox.Show("插入插补数据失败！");
                return;
            }
        }

        private void button_StartCrd_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            iRes = MultiCardCS_1.GA_CrdStart(1, 0);

            if (0 == iRes)
            {
                MessageBox.Show("启动坐标系成功！");
            }
            else
            {
                MessageBox.Show("启动坐标系失败！");
                return;
            }
        }

        private void button_StopCrd_Click(object sender, EventArgs e)
        {
            MultiCardCS_1.GA_Stop(0X10000, 0X10000);
        }

        private void labelUserSegNum_Click(object sender, EventArgs e)
        {

        }

        private void trackBarOverRide_Scroll(object sender, EventArgs e)
        {
            double dOverRide = 0;

            dOverRide = trackBarOverRide.Value/100.0;

            MultiCardCS_1.GA_SetOverride(1, dOverRide);
        }

        private void button26_Click_1(object sender, EventArgs e)
        {
            int iRes = 0;
            int iClock = 0;

            short nValue = 10000;

            byte[] Data = new byte[128];

            short nLength = 0;

            short nAxisNum = (short)(comboBoxAxisSel.SelectedIndex + 1);

            MultiCardCS_1.GA_ClrSts(nAxisNum, 1);


            MultiCardCS_1.GA_SetExtDiFilter(0, 30, 0X0002);
            MultiCardCS_1.GA_SetCaptureMode(1, 215);

            short nStatus = 0;
            int Value = 0;
            int Clock = 0;
            MultiCardCS_1.GA_GetCaptureStatus(1, ref nStatus, ref Value, 1, ref Clock);
        }

        private void button28_Click(object sender, EventArgs e)
        {
            int iRes = 0;

            short nAxisNum = (short)(comboBoxAxisSel3.SelectedIndex + 1);
/*
typedef struct _AxisHomeParm{
	short		nHomeMode;					//1--HOME回原点	2--HOME加Index回原点	
	short		nHomeDir;					//回零方向，1-正向回零，0-负向回零
	long        lOffset;                    //回零偏移，回到零位后再走一个Offset作为零位

	double		dHomeRapidVel;			//回零快移速度，单位：Pluse/ms
	double		dHomeLocatVel;			//回零定位速度，单位：Pluse/ms
	double		dHomeIndexVel;			//回零寻找INDEX速度，单位：Pluse/ms
	double      dHomeAcc;               //回零使用的加速度

    long ulHomeIndexDis;           //找Index最大距离
	long ulHomeBackDis;            //回零时，第一次碰到零位后的回退距离
	unsigned short nDelayTimeBeforeZero;    //位置清零前，延时时间，单位ms
    unsigned long ulHomeMaxDis;//回零最大寻找范围，单位脉冲
}TAxisHomePrm;
*/
            MultiCardCS.MultiCardCS.TAxisHomePrm AxisHomePrm;

            AxisHomePrm.nHomeMode = 1;
            AxisHomePrm.nHomeDir = 0;
            AxisHomePrm.lOffset = 0;
            AxisHomePrm.dHomeRapidVel = 1;
            AxisHomePrm.dHomeLocatVel = 1;
            AxisHomePrm.dHomeIndexVel = 1;
            AxisHomePrm.dHomeAcc = 1;
            
            AxisHomePrm.nDelayTimeBeforeZero = 3000;
            
            AxisHomePrm.ulHomeBackDis = 0;
            AxisHomePrm.ulHomeIndexDis = 0;
            AxisHomePrm.ulHomeMaxDis = 0;

            MultiCardCS_1.GA_HomeSetPrm(nAxisNum, ref AxisHomePrm);
            iRes += MultiCardCS_1.GA_HomeStart(nAxisNum);
        }

        private void button29_Click(object sender, EventArgs e)
        {
            short nAxisNum = (short)(comboBoxAxisSel3.SelectedIndex + 1);

            MultiCardCS_1.GA_HomeStop(nAxisNum);
        }

        private void button30_Click(object sender, EventArgs e)
        {
            short nAxisNum = (short)(comboBoxAxisSel.SelectedIndex + 1);

            MultiCardCS_1.GA_AxisOn(nAxisNum);
        }

        private void button31_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            short nStatus = 0;
            int ValueType = 0;
            int pClock = 0;

            short nAxisNum = (short)(comboBoxAxisSel.SelectedIndex + 1);

            MultiCardCS_1.GA_AxisOff(nAxisNum);

            MultiCardCS_1.GA_GetCaptureStatus(2, ref nStatus, ref ValueType, 1, ref pClock);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            MultiCardCS_1.GA_StartDebugLog(0);


            /*
            函数名：	int GA_StartWatch(long lAxisMask,long lPackageCountFlag,long lUserSegNumFlag,long lReserve,char *FilePath)
            函数说明：	lAxisMask:轴掩码，一个位代表一个轴，bit0代表轴1，bit1代表轴2......如1代表打印轴1，3代表打印轴1和轴2，4代表打印轴3
			            lPackageCountFlag:是否打印包顺序，0不打印，1打印
			            lUserSegNumFlag:是否打印用户自定义段号，0不打印，1打印
			            lReserve:保留
			            FilePath:Watch数据保存路径,如"D:\\",则保存在D盘根目录，如果为空""，则默认保存在dll所在路径
            参数说明：	无
            返回值：	0成功，其他失败
            注意事项：	无
            */
            MultiCardCS_1.GA_StartWatch(3,1,0,0,"D:\\");
        }

        private void button32_Click(object sender, EventArgs e)
        {
            //这里先退出手轮模式，否则修改手轮比例会失败
            MultiCardCS_1.GA_EndHandwheel(1);

            //说明：MC_StartHandwheel函数共9个参数，实际使用中，只需要关心第1个参数（轴号），第3个参数（比例分母），第4个参数（比例分子）
            //第一个参数轴号决定了哪个轴进入手轮模式，第3和第4个参数比例分子和比例分母则决定了手轮摇动过程中，该轴的移动速度。如果比例分子和比例分母都是1，则手轮摇动1格，电机走1个脉冲
            //其他6个参数固定，不用修改。
            MultiCardCS_1.GA_StartHandwheel(1, 9, int.Parse(textBoxMPG_Alpha.Text), int.Parse(textBoxMPG_Beta.Text), 0, 1, 1, 50, 0);

            //轴选和倍率部分参见定时器函数OnTimer
            //X/Y/Z/A/B/X1/X10/X100这些逻辑，是自己组合的。

            //比如，读取到X轴的轴选IO信号时，将X轴切换到手轮模式，读取到Y轴的轴选IO信号时，首先将X轴退出手轮模式后，将Y轴设置为手轮模式。

            //比如：读取到X1倍率信号时，将比例分子分母设置为1：1，当读取到X10信号时，将比例分子分母设置为1：10(根据自己机床的螺距、电机每圈脉冲个数自行设置，不一定是1：10)
            //注意要先退出手轮模式，再进入手轮模式，比例分子和比例分母才能生效
        }

        private void button33_Click(object sender, EventArgs e)
        {
            MultiCardCS_1.GA_EndHandwheel(1);
        }

        private void button34_Click(object sender, EventArgs e)
        {
            int iRes = 0;

            iRes = MultiCardCS_1.GA_PrfPt(1, 0);
        }

        private void button35_Click(object sender, EventArgs e)
        {
            int iRes = 0;

            //这里声明了一个长度未100的数据，然后随便写了一个算法，就是一个普通的三角形（可以根据自己的需要修改为正玄波等等）
            short[] nData = new short[100];

            nData[0] = 1;
            nData[1] = 2;
            nData[2] = 1;
            nData[3] = 3;
            nData[4] = 7;
            nData[5] = 5;
            nData[6] = 2;

            //这里设计的比较简单，直接把数据发送给控制卡,实际应用中，要查询PT空闲空间大小，否则就会发送数据失败
            iRes = MultiCardCS_1.GA_PtData(1, ref nData[0], 50, dDataID);


            for (short i = 0; i < 50; i++)
            {
                nData[i] = i;
            }
            for (short i = 50; i < 100; i++)
            {
                nData[i] = (short)(100 - i);
            }


            //这里设计的比较简单，直接把数据发送给控制卡,实际应用中，要查询PT空闲空间大小，否则就会发送数据失败
            iRes = MultiCardCS_1.GA_PtData(1, ref nData[0], 50, dDataID);

            //这个ID标识每次必须变化，否则板卡会认为发送的是重复包，会丢弃
            dDataID++;
        }

        private void button36_Click(object sender, EventArgs e)
        {
            //这个PTStart的参数是按位表示每个轴的，比如启动轴1就是1，启动轴2就是2，启动轴3就是4，启动轴4就是8......
            //如果要同时启动轴1和轴4，那就是8+1 = 9
            MultiCardCS_1.GA_PtStart(1);
        }

        private void groupBox31_Enter(object sender, EventArgs e)
        {

        }

        //强制比较输出脉冲口输出高电平
        private void button37_Click(object sender, EventArgs e)
        {
            /*
            函数名：    int MC_CmpPluse(short nChannelMask, short nPluseType1, short nPluseType2, short nTime1,short nTime2, short nTimeFlag1, short nTimeFlag2)
            函数说明：	设置比较器输出IO立即输出指定电平或者脉冲
            参数说明：	nChannelMask,bit0表示通道1，bit1表示通道2
                        nPluseType1：通道1输出类型，0低电平1高电平2脉冲
			            nPluseType2：通道2输出类型，0低电平1高电平2脉冲
                        nTime1：通道1脉冲持续时间，输出类型为脉冲时该参数有效
			            nTime2：通道2脉冲持续时间，输出类型为脉冲时该参数有效
			            nTimeFlag1：比较器1的脉冲时间单位，0us，1ms
			            nTimeFlag2：比较器2的脉冲时间单位，0us，1ms
            返回值：	0成功，其他：失败

            注意事项：  该函数可以强制比较输出端口输出高电平/低电平/脉冲，方便用户测试硬件是否正常输出
            */
            MultiCardCS_1.GA_CmpPluse(1, 1, 0, 0, 0, 0, 0);
        }

        //强制比较输出脉冲口输出低电平
        private void button38_Click(object sender, EventArgs e)
        {
            MultiCardCS_1.GA_CmpPluse(1, 0, 0, 0, 0, 0, 0);
        }

        //强制比较输出脉冲口输出一个脉冲
        private void button39_Click(object sender, EventArgs e)
        {
            //第一个参数代表通道1，第二个参数代表类型为脉冲，第3个参数是通道2类型（用不到），第4个参数1000代表时间，第5个参数是通道2的时间（用不到）
            //第6个参数代表脉冲时间的单位，0是微秒，1是毫秒。第7个参数是通道2的脉冲时间单位（用不到）
            MultiCardCS_1.GA_CmpPluse(1, 2, 0, 1000, 0, (short)comboBoxTimeUnitSel.SelectedIndex, 0);
        }

        private void button40_Click(object sender, EventArgs e)
        {
            int [] BufData1 = new int[10];
            int [] BufData2 = new int[10];

            //写入比较位置，10000，20000，30000........
            for(int i=0; i<10; i++)
            {
                BufData1[i] = 10000*(i+1);
            }

            //关闭轴1编码器
            MultiCardCS_1.GA_EncOff(1);

            //清零轴1位置
            MultiCardCS_1.GA_ZeroPos(1, 1);
/*
函数名：    int MC_CmpBufData(short nCmpEncodeNum, short nPluseType, short nStartLevel, short nTime, long *pBuf1, short nBufLen1, long *pBuf2, short nBufLen2,short nAbsPosFlag,short nTimerFlag)
函数说明：	向比较器缓冲区发送比较数据
参数说明：	nCmpEncodeNum:轴号
			nPluseType：2 表示输出脉冲，1表示反转电平。
			nStartLevel：按位设置比较器输出的初始电平，bit0比较器11，bit1比较器2； 0：表示初始为低电平； 1：表示初始为高电平
			nTime:输出脉冲时，该参数用来设定脉冲输出宽度，取值范围：[1, 65535]，单位：ms，输出电平时，该参数无效
			pBuf1:比较器1数据缓冲区，位置值为相对当前位置的距离
			nBufLen1:比较器1数据缓冲区长度，0~128
			pBuf2:比较器2数据缓冲区，位置值为相对当前位置的距离
			nBufLen2:比较器2数据缓冲区长度，0~128
			nAbsPosFlag：0：相对当前位置1：绝对位置
			nTimerFlag：脉冲时间单位，0us，1ms
返回值：	0成功，其他：失败

注意事项：  无
*/
            //这里为了便于观察，脉冲持续时间为100毫秒，可根据情况自行修改
            MultiCardCS_1.GA_CmpBufData(1, 2, 0, 100, ref BufData1[0], 10, ref BufData2[0], 10, 1, 1);
        }

        private void buttonEncOnOff_Click(object sender, EventArgs e)
        {
            short nAxisNum = (short)(comboBoxAxisSel3.SelectedIndex + 1);
            MultiCardCS_1.GA_EncOff(nAxisNum);
        }

        private void groupBox32_Enter(object sender, EventArgs e)
        {

        }

        private void button41_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            /*
            函数名：    int GA_UartConfig(unsigned short nUartNum,	unsigned long uLBaudRate,unsigned short nDataLength,unsigned short nVerifyType,unsigned short nStopBitLen)
            函数说明：	设置串口波特率、数据长度、校验和、停止位长度等通讯相关参数
            参数说明：	
                        nUartNum;//串口号,485固定为3
                        uLBaudRate;//波特率
                        nDataLength;//数据长度
                        nVerifyType;//校验类型0，无校验，1：奇校验，2：偶校验
                        nStopBitLen;//停止位，该参数固定为0，0代表一个停止位
            返回值：	0成功，其他失败

            注意事项：  无
            */
            iRes = MultiCardCS_1.GA_UartConfig(3, 9600, 8, 0, 0);

            if (iRes == 0)
            {
                MessageBox.Show("设置串口成功！");
                OpenFlag = 1;
            }
            else
            {
                MessageBox.Show("设置串口失败！");
            }
        }

        private void button42_Click(object sender, EventArgs e)
        {
            byte[] Temp = new byte[8];

            //任意数据
            Temp[0] = 0X01;
            Temp[1] = 0X03;
            Temp[2] = 0X05;
            Temp[3] = 0X02;
            Temp[4] = 0X04;
            Temp[5] = 0X06;
            Temp[6] = 0X11;
            Temp[7] = 0X22;

            MultiCardCS_1.GA_SendEthToUartString(3, ref Temp[0], 8);
        }

        private void button43_Click(object sender, EventArgs e)
        {
            byte[] Temp = new byte[128];
            short nLength = 0;

            MultiCardCS_1.GA_ReadUartToEthString(3, ref Temp[0], ref nLength);

            string strText = string.Format("接收到{0:G}字节", nLength);
            labelRX.Text = strText;
        }

        private void button44_Click(object sender, EventArgs e)
        {
            int iRes = 0;

            MultiCardCS_1.GA_Stop(0x0001 << 16, 0x0001 << 16);

            //---------------------------------------建立坐标系------------------------------------------------
            //坐标系参数初始化
            MultiCardCS.MultiCardCS.TCrdPrm crdPrm;

            //设置坐标系最小匀速时间为0
            crdPrm.evenTime = 0;

            //设置坐标系维数为3
            crdPrm.dimension = 2;

            //设置坐标系规划对应轴通道，X对应轴1，Y对应轴2，Z对应轴3
            crdPrm.profile = new short[8];
            crdPrm.profile[0] = 1;
            crdPrm.profile[1] = 2;
            crdPrm.profile[2] = 3;
            crdPrm.profile[3] = 4;
            crdPrm.profile[4] = 5;
            crdPrm.profile[5] = 6;
            crdPrm.profile[6] = 7;
            crdPrm.profile[7] = 8;


            //设置坐标系最大加速度为0.2脉冲/毫秒^2
            crdPrm.synAccMax = 1;
            //设置坐标系最大速度为100脉冲/毫秒
            crdPrm.synVelMax = 1000;

            //设置坐标系原点
            //设置原点坐标值标志,0:默认当前规划位置为原点位置;1:用户指定原点位置
            crdPrm.setOriginFlag = 1;

            crdPrm.originPos = new int[8];
            crdPrm.originPos[0] = int.Parse(textBoxOrgX.Text);
            crdPrm.originPos[1] = int.Parse(textBoxOrgY.Text);
            crdPrm.originPos[2] = int.Parse(textBoxOrgZ.Text);

            //设置坐标系参数，建立坐标系
            iRes = MultiCardCS_1.GA_SetCrdPrm(1, ref crdPrm);

            if (0 == iRes)
            {
                MessageBox.Show("建立坐标系成功！");
            }
            else
            {
                MessageBox.Show("建立坐标系失败！");
                //return;
            }
            //------------------------------------------------------------------------------------------------


            for (int i = 0; i < 10; i++)
            {
                iRes = MultiCardCS_1.GA_LnXY(1, 10000, 0, 50, 1, 0, 0, 0);
                iRes = MultiCardCS_1.GA_LnXY(1, 0, 0, 50, 1, 0, 0, 1);
            }

            iRes += MultiCardCS_1.GA_CrdData(1, 0, 0);

            iRes = MultiCardCS_1.GA_CrdStart(1, 0);
        }
    }
}
