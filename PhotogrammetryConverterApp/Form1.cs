using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using PilotGaea.Serialize;
using PilotGaea.TMPEngine;
using PilotGaea.Geometry;

namespace PhotogrammetryConverterApp
{
    public partial class Form1 : Form
    {
        CPhotogrammetryModelMaker m_Maker = null;
        Stopwatch m_Stopwatch = new Stopwatch();

        public Form1()
        {
            InitializeComponent();

            //加入功能列表
            List<string> featureNames = new List<string>();
            featureNames.Add("基本");
            featureNames.Add("來源3MX");
            featureNames.Add("輸出OGC I3S");
            featureNames.Add("輸出OGC 3DTiles");
            featureNames.Add("變換-平移");
            featureNames.Add("變換-旋轉");
            featureNames.Add("變換-縮放");
            featureNames.Add("遮罩-內縮");
            featureNames.Add("遮罩-無內縮");
            featureNames.Add("刪除指定區域");
            featureNames.Add("添加額外模型");
            featureNames.Add("單緒");
            comboBox_Features.Items.AddRange(featureNames.ToArray());
            comboBox_Features.SelectedIndex = 0;
        }

        private void button_Start_Click(object sender, EventArgs e)
        {
            EnableUI(false);

            //將來源資料輸出成PhotogrammetryModel圖層
            System.Environment.CurrentDirectory = @"C:\Program Files\PilotGaea\TileMap";//為了順利存取安裝目錄下的相關DLL
            m_Maker = new CPhotogrammetryModelMaker();
            //設定必要參數
            //     輸出類型。
            EXPORT_TYPE exportType = EXPORT_TYPE.LET_DB;
            //     圖層名稱，用於將輸出圖層命名。
            string layerName = "test";
            //     指定執行Create的資料庫路徑。
            string destPath = string.Format(@"{0}\..\output\photogrammetrymodel_maker.DB", Application.StartupPath);
            //     來源資料檔案路徑。
            string srcPath = string.Format(@"{0}\..\data\photogrammetrymodel_maker\LOD\LODTreeExport.xml", Application.StartupPath);
            //     高程資料庫路徑。
            string terrainPath = string.Format(@"{0}\..\data\terrain_maker\terrain.DB", Application.StartupPath);
            //     於高程資料庫內指定的高程圖層名稱。
            string terrainName = "terrain";
            //設定進階參數(可選)
            //1. 圖層位移，透過offset參數指定對三軸的位移量
            //     圖層位移，單位為公尺。
            GeoPoint offset = null;
            //     圖層對Z軸的旋轉角度(0~359)。
            double rotationAngle = 0;
            //     圖層放大比例，預設為1。
            double scale = 1;
            //2. 遮罩功能，將isDoMaskLayer參數設為true開啟遮罩。
            //     是否執行遮罩功能。
            bool isDoMaskLayer = false;
            //     遮罩的縮放值，單位為公尺，僅開啟執行遮罩功能時有用
            double maskLayerOffsetting = 0;
            //3. 模型邊緣拉伸功能，將isDoExtrudeEdge設為true來開啟。
            bool isDoExtrudeEdge = false;
            //4. 添加屬性，透過自訂屬性的shp檔，將各多邊形的屬性添加到圖層內
            //     添加屬性shp檔的檔案路徑。
            string attrSHPFileName = "";
            //     添加屬性shp檔的EPSG。
            long attrSHPFileEPSG = 4326;
            //5. 刪除指定區域，在removalPolygons參數透過shp檔指定要刪除的圖層區域，shp檔SRS只支援4326。
            //     刪除指定shp檔內的多邊形區域。
            GeoPolygonSet[] removalPolygons = null;
            //6. 添加額外模型功能，可透過additionalModel相關參數添加額外模型到圖層中，
            //     額外模型的檔案路徑。
            string[] additionalModelFileNames = null;
            //     額外模型的插入點座標，EPSG4326描述。
            GeoPoint[] additionalModelPositions = null;
            //     額外模型對三軸的旋轉，單位為弧度。
            GeoPoint[] additionalModelRotations = null;
            //     額外模型的縮放值，1為原始大小。
            GeoPoint[] additionalModelScales = null;
            //轉檔使用執行緒數量，負數為使用單緒，0為自動分配。
            int maxThreadCount = 0;

            //監聽轉檔事件
            m_Maker.CreateLayerCompleted += M_Maker_CreateLayerCompleted;
            m_Maker.ProgressMessageChanged += M_Maker_ProgressMessageChanged;
            m_Maker.ProgressPercentChanged += M_Maker_ProgressPercentChanged;

            //設定進階參數
            switch (comboBox_Features.SelectedIndex)
            {
                case 0://"基本"
                    break;
                case 1://"來源3MX"
                    srcPath = string.Format(@"{0}\..\data\photogrammetrymodel_maker\3MX\church_3mx.3mx", Application.StartupPath);
                    destPath = string.Format(@"{0}\..\output\photogrammetrymodel_maker_3MX.DB", Application.StartupPath);
                    break;
                case 2://"輸出OGC I3S"
                    exportType = EXPORT_TYPE.LET_OGCI3S;
                    layerName = "photogrammetrymodel_maker_ogci3s";
                    //會在destPath目錄下產生layerName.slpk
                    break;
                case 3://"輸出OGC 3DTiles
                    exportType = EXPORT_TYPE.LET_OGC3DTILES;
                    layerName = "photogrammetrymodel_maker_ogc3dtiles";
                    //會在destPath目錄下產生layerName資料夾
                    break;
                case 4://"變換-平移"
                    break;
                case 5://"變換-旋轉"
                    break;
                case 6://"變換-縮放"
                    break;
                case 7://"遮罩-內縮"
                    break;
                case 8://"遮罩-無內縮"
                    break;
                case 9://"刪除指定區域"
                    break;
                case 10://"添加額外模型"
                    break;
                case 11://"單緒"
                    maxThreadCount = 1;
                    break;
            }

            m_Stopwatch.Restart();
            //開始非同步轉檔
            bool ret = m_Maker.Create(exportType, layerName, destPath, srcPath, terrainPath, terrainName,
                offset, rotationAngle, scale,
                isDoMaskLayer, maskLayerOffsetting, isDoExtrudeEdge,
                attrSHPFileName, attrSHPFileEPSG,
                removalPolygons,
                additionalModelFileNames, additionalModelPositions, additionalModelRotations, additionalModelScales,
                maxThreadCount
                );

            string message = string.Format("參數檢查{0}", (ret ? "通過" : "失敗"));
            listBox_Main.Items.Add(message);
        }

        private void M_Maker_ProgressMessageChanged(string message)
        {
            listBox_Main.Items.Add(message);
        }

        private void M_Maker_ProgressPercentChanged(double percent)
        {
            progressBar_Main.Value = Convert.ToInt32(percent);
        }


        private void M_Maker_CreateLayerCompleted(string layerName, bool isSuccess, string errorMessage)
        {
            m_Stopwatch.Stop();
            string message = string.Format("轉檔{0}", (isSuccess ? "成功" : "失敗"));
            listBox_Main.Items.Add(message);
            message = string.Format("耗時{0}分。", m_Stopwatch.Elapsed.TotalMinutes.ToString("0.00"));
            listBox_Main.Items.Add(message);
        }

        private void EnableUI(bool enable)
        {
            button_Start.Enabled = enable;
            comboBox_Features.Enabled = enable;
        }
    }
}