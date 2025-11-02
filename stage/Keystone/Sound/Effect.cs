//using System;
//using System.Collections;
//using System.ComponentModel;
//using System.Drawing;
//using System.Windows.Forms;
//using Microsoft.DirectX;
//using Microsoft.DirectX.DirectSound;

//public class MainForm : Form
//{
//    #region Control declarations

//    private ListBox listboxEffects;
//    private RadioButton radiobuttonRadioSine;
//    private Button buttonOk;
//    private GroupBox groupboxFrame;
//    private Label labelParamName1;
//    private Label labelParamValue1;
//    private TrackBar trackbarSlider1;
//    private Label labelParamMin1;
//    private Label labelParamMax1;
//    private Label labelParamName2;
//    private Label labelParamValue2;
//    private TrackBar trackbarSlider2;
//    private Label labelParamMin2;
//    private Label labelParamMax2;
//    private Label labelParamName3;
//    private Label labelParamValue3;
//    private TrackBar trackbarSlider3;
//    private Label labelParamMin3;
//    private Label labelParamMax3;
//    private Label labelParamName4;
//    private Label labelParamValue4;
//    private TrackBar trackbarSlider4;
//    private Label labelParamMin4;
//    private Label labelParamMax4;
//    private Label labelParamName5;
//    private Label labelParamValue5;
//    private TrackBar trackbarSlider5;
//    private Label labelParamMin5;
//    private Label labelParamMax5;
//    private Label labelParamName6;
//    private Label labelParamValue6;
//    private TrackBar trackbarSlider6;
//    private Label labelParamMin6;
//    private Label labelParamMax6;
//    private RadioButton radiobuttonTriangle;
//    private RadioButton radiobuttonSquare;
//    private GroupBox groupboxFrameWaveform;
//    private Button buttonOpen;
//    private Label labelTextFilename;
//    private Label labelStatic2;
//    private Label labelTextStatus;
//    private CheckBox checkboxLoop;
//    private Button buttonPlay;
//    private Button buttonStop;
//    private Label labelStatic3;
//    private GroupBox groupboxFramePhase;
//    private Label labelStatic4;
//    private RadioButton radiobuttonRadioNeg180;
//    private RadioButton radiobuttonRadioNeg90;
//    private RadioButton radiobuttonRadioZero;
//    private RadioButton radiobuttonRadio90;
//    private RadioButton radiobuttonRadio180;
//    private GroupBox groupboxEffects;

//    #endregion

//    private struct EffectInfo
//    {
//        public EffectDescription description;
//        public object EffectSettings;
//        public object Effect;
//    } ;

//    private ArrayList effectDescription = new ArrayList();
//    private SecondaryBuffer applicationBuffer = null;
//    private Device applicationDevice = null;
//    private string fileName = string.Empty;
//    private string path = string.Empty;
//    private bool shouldLoop = false;
//    private Button buttonDelete;
//    private Timer timer1;
//    private IContainer components;
//    private ComboBox comboEffects;
//    private int currentIndex = 0;
//    private bool isIgnoringSettings = false;

//    [STAThread]
//    public static void Main()
//    {
//        try
//        {
//            using (MainForm f = new MainForm())
//            {
//                Application.Run(f);
//            }
//        }
//        catch
//        {
//        }
//    }

//    public MainForm()
//    {
//        InitializeComponent();
//        InitDirectSound();
//        ClearUI(true);
//    }

//    #region InitializeComponent code

//    private void InitializeComponent()
//    {
//        components = new Container();
//        buttonOk = new Button();
//        groupboxFrame = new GroupBox();
//        labelParamName1 = new Label();
//        labelParamValue1 = new Label();
//        trackbarSlider1 = new TrackBar();
//        labelParamMin1 = new Label();
//        labelParamMax1 = new Label();
//        labelParamName2 = new Label();
//        labelParamValue2 = new Label();
//        trackbarSlider2 = new TrackBar();
//        labelParamMin2 = new Label();
//        labelParamMax2 = new Label();
//        labelParamName3 = new Label();
//        labelParamValue3 = new Label();
//        trackbarSlider3 = new TrackBar();
//        labelParamMin3 = new Label();
//        labelParamMax3 = new Label();
//        labelParamName4 = new Label();
//        labelParamValue4 = new Label();
//        trackbarSlider4 = new TrackBar();
//        labelParamMin4 = new Label();
//        labelParamMax4 = new Label();
//        labelParamName5 = new Label();
//        labelParamValue5 = new Label();
//        trackbarSlider5 = new TrackBar();
//        labelParamMin5 = new Label();
//        labelParamMax5 = new Label();
//        labelParamName6 = new Label();
//        labelParamValue6 = new Label();
//        trackbarSlider6 = new TrackBar();
//        labelParamMin6 = new Label();
//        labelParamMax6 = new Label();
//        radiobuttonTriangle = new RadioButton();
//        radiobuttonSquare = new RadioButton();
//        radiobuttonRadioSine = new RadioButton();
//        groupboxFrameWaveform = new GroupBox();
//        buttonOpen = new Button();
//        labelTextFilename = new Label();
//        labelStatic2 = new Label();
//        labelTextStatus = new Label();
//        checkboxLoop = new CheckBox();
//        buttonPlay = new Button();
//        buttonStop = new Button();
//        labelStatic3 = new Label();
//        labelStatic4 = new Label();
//        radiobuttonRadioNeg180 = new RadioButton();
//        radiobuttonRadioNeg90 = new RadioButton();
//        radiobuttonRadioZero = new RadioButton();
//        radiobuttonRadio90 = new RadioButton();
//        radiobuttonRadio180 = new RadioButton();
//        groupboxFramePhase = new GroupBox();
//        groupboxEffects = new GroupBox();
//        buttonDelete = new Button();
//        listboxEffects = new ListBox();
//        comboEffects = new ComboBox();
//        timer1 = new Timer(components);
//        (trackbarSlider1).BeginInit();
//        (trackbarSlider2).BeginInit();
//        (trackbarSlider3).BeginInit();
//        (trackbarSlider4).BeginInit();
//        (trackbarSlider5).BeginInit();
//        (trackbarSlider6).BeginInit();
//        groupboxFrameWaveform.SuspendLayout();
//        groupboxFramePhase.SuspendLayout();
//        groupboxEffects.SuspendLayout();
//        SuspendLayout();
//        // 
//        // buttonOk
//        // 
//        buttonOk.Location = new Point(87, 432);
//        buttonOk.Name = "buttonOk";
//        buttonOk.Size = new Size(67, 23);
//        buttonOk.TabIndex = 0;
//        buttonOk.Text = "E&xit";
//        buttonOk.Click += new EventHandler(buttonOk_Click);
//        // 
//        // groupboxFrame
//        // 
//        groupboxFrame.Location = new Point(165, 76);
//        groupboxFrame.Name = "groupboxFrame";
//        groupboxFrame.Size = new Size(525, 380);
//        groupboxFrame.TabIndex = 1;
//        groupboxFrame.TabStop = false;
//        groupboxFrame.Text = "Parameters";
//        // 
//        // labelParamName1
//        // 
//        labelParamName1.Location = new Point(169, 112);
//        labelParamName1.Name = "labelParamName1";
//        labelParamName1.Size = new Size(117, 13);
//        labelParamName1.TabIndex = 2;
//        labelParamName1.TextAlign = ContentAlignment.TopRight;
//        // 
//        // labelParamValue1
//        // 
//        labelParamValue1.BorderStyle = BorderStyle.Fixed3D;
//        labelParamValue1.Location = new Point(294, 112);
//        labelParamValue1.Name = "labelParamValue1";
//        labelParamValue1.Size = new Size(67, 16);
//        labelParamValue1.TabIndex = 3;
//        labelParamValue1.Text = "Value";
//        labelParamValue1.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // trackbarSlider1
//        // 
//        trackbarSlider1.Location = new Point(426, 112);
//        trackbarSlider1.Name = "trackbarSlider1";
//        trackbarSlider1.Size = new Size(195, 45);
//        trackbarSlider1.TabIndex = 4;
//        trackbarSlider1.Text = "Slider1";
//        trackbarSlider1.TickStyle = TickStyle.None;
//        trackbarSlider1.Scroll += new EventHandler(trackbarSliderScroll);
//        // 
//        // labelParamMin1
//        // 
//        labelParamMin1.Location = new Point(366, 112);
//        labelParamMin1.Name = "labelParamMin1";
//        labelParamMin1.Size = new Size(60, 16);
//        labelParamMin1.TabIndex = 5;
//        labelParamMin1.Text = "min";
//        labelParamMin1.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // labelParamMax1
//        // 
//        labelParamMax1.Location = new Point(627, 120);
//        labelParamMax1.Name = "labelParamMax1";
//        labelParamMax1.Size = new Size(52, 13);
//        labelParamMax1.TabIndex = 6;
//        labelParamMax1.Text = "max";
//        labelParamMax1.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // labelParamName2
//        // 
//        labelParamName2.Location = new Point(169, 160);
//        labelParamName2.Name = "labelParamName2";
//        labelParamName2.Size = new Size(117, 13);
//        labelParamName2.TabIndex = 7;
//        labelParamName2.TextAlign = ContentAlignment.TopRight;
//        // 
//        // labelParamValue2
//        // 
//        labelParamValue2.BorderStyle = BorderStyle.Fixed3D;
//        labelParamValue2.Location = new Point(294, 160);
//        labelParamValue2.Name = "labelParamValue2";
//        labelParamValue2.Size = new Size(67, 16);
//        labelParamValue2.TabIndex = 8;
//        labelParamValue2.Text = "Value";
//        labelParamValue2.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // trackbarSlider2
//        // 
//        trackbarSlider2.Location = new Point(426, 163);
//        trackbarSlider2.Name = "trackbarSlider2";
//        trackbarSlider2.Size = new Size(195, 45);
//        trackbarSlider2.TabIndex = 9;
//        trackbarSlider2.Text = "Slider1";
//        trackbarSlider2.TickStyle = TickStyle.None;
//        trackbarSlider2.Scroll += new EventHandler(trackbarSliderScroll);
//        // 
//        // labelParamMin2
//        // 
//        labelParamMin2.Location = new Point(366, 160);
//        labelParamMin2.Name = "labelParamMin2";
//        labelParamMin2.Size = new Size(60, 16);
//        labelParamMin2.TabIndex = 10;
//        labelParamMin2.Text = "min";
//        labelParamMin2.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // labelParamMax2
//        // 
//        labelParamMax2.Location = new Point(627, 168);
//        labelParamMax2.Name = "labelParamMax2";
//        labelParamMax2.Size = new Size(52, 13);
//        labelParamMax2.TabIndex = 11;
//        labelParamMax2.Text = "max";
//        labelParamMax2.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // labelParamName3
//        // 
//        labelParamName3.Location = new Point(169, 208);
//        labelParamName3.Name = "labelParamName3";
//        labelParamName3.Size = new Size(117, 13);
//        labelParamName3.TabIndex = 12;
//        labelParamName3.TextAlign = ContentAlignment.TopRight;
//        // 
//        // labelParamValue3
//        // 
//        labelParamValue3.BorderStyle = BorderStyle.Fixed3D;
//        labelParamValue3.Location = new Point(294, 208);
//        labelParamValue3.Name = "labelParamValue3";
//        labelParamValue3.Size = new Size(67, 16);
//        labelParamValue3.TabIndex = 13;
//        labelParamValue3.Text = "Value";
//        labelParamValue3.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // trackbarSlider3
//        // 
//        trackbarSlider3.Location = new Point(426, 208);
//        trackbarSlider3.Name = "trackbarSlider3";
//        trackbarSlider3.Size = new Size(195, 45);
//        trackbarSlider3.TabIndex = 14;
//        trackbarSlider3.Text = "Slider1";
//        trackbarSlider3.TickStyle = TickStyle.None;
//        trackbarSlider3.Scroll += new EventHandler(trackbarSliderScroll);
//        // 
//        // labelParamMin3
//        // 
//        labelParamMin3.Location = new Point(366, 208);
//        labelParamMin3.Name = "labelParamMin3";
//        labelParamMin3.Size = new Size(60, 16);
//        labelParamMin3.TabIndex = 15;
//        labelParamMin3.Text = "min";
//        labelParamMin3.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // labelParamMax3
//        // 
//        labelParamMax3.Location = new Point(627, 216);
//        labelParamMax3.Name = "labelParamMax3";
//        labelParamMax3.Size = new Size(52, 13);
//        labelParamMax3.TabIndex = 16;
//        labelParamMax3.Text = "max";
//        labelParamMax3.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // labelParamName4
//        // 
//        labelParamName4.Location = new Point(169, 256);
//        labelParamName4.Name = "labelParamName4";
//        labelParamName4.Size = new Size(117, 13);
//        labelParamName4.TabIndex = 17;
//        labelParamName4.TextAlign = ContentAlignment.TopRight;
//        // 
//        // labelParamValue4
//        // 
//        labelParamValue4.BorderStyle = BorderStyle.Fixed3D;
//        labelParamValue4.Location = new Point(294, 256);
//        labelParamValue4.Name = "labelParamValue4";
//        labelParamValue4.Size = new Size(67, 16);
//        labelParamValue4.TabIndex = 18;
//        labelParamValue4.Text = "Value";
//        labelParamValue4.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // trackbarSlider4
//        // 
//        trackbarSlider4.Location = new Point(426, 256);
//        trackbarSlider4.Name = "trackbarSlider4";
//        trackbarSlider4.Size = new Size(195, 45);
//        trackbarSlider4.TabIndex = 19;
//        trackbarSlider4.Text = "Slider1";
//        trackbarSlider4.TickStyle = TickStyle.None;
//        trackbarSlider4.Scroll += new EventHandler(trackbarSliderScroll);
//        // 
//        // labelParamMin4
//        // 
//        labelParamMin4.Location = new Point(366, 256);
//        labelParamMin4.Name = "labelParamMin4";
//        labelParamMin4.Size = new Size(60, 16);
//        labelParamMin4.TabIndex = 20;
//        labelParamMin4.Text = "min";
//        labelParamMin4.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // labelParamMax4
//        // 
//        labelParamMax4.Location = new Point(627, 256);
//        labelParamMax4.Name = "labelParamMax4";
//        labelParamMax4.Size = new Size(52, 13);
//        labelParamMax4.TabIndex = 21;
//        labelParamMax4.Text = "max";
//        labelParamMax4.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // labelParamName5
//        // 
//        labelParamName5.Location = new Point(169, 304);
//        labelParamName5.Name = "labelParamName5";
//        labelParamName5.Size = new Size(117, 13);
//        labelParamName5.TabIndex = 22;
//        labelParamName5.TextAlign = ContentAlignment.TopRight;
//        // 
//        // labelParamValue5
//        // 
//        labelParamValue5.BorderStyle = BorderStyle.Fixed3D;
//        labelParamValue5.Location = new Point(294, 304);
//        labelParamValue5.Name = "labelParamValue5";
//        labelParamValue5.Size = new Size(67, 16);
//        labelParamValue5.TabIndex = 23;
//        labelParamValue5.Text = "Value";
//        labelParamValue5.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // trackbarSlider5
//        // 
//        trackbarSlider5.Location = new Point(426, 304);
//        trackbarSlider5.Name = "trackbarSlider5";
//        trackbarSlider5.Size = new Size(195, 45);
//        trackbarSlider5.TabIndex = 24;
//        trackbarSlider5.Text = "Slider1";
//        trackbarSlider5.TickStyle = TickStyle.None;
//        trackbarSlider5.Scroll += new EventHandler(trackbarSliderScroll);
//        // 
//        // labelParamMin5
//        // 
//        labelParamMin5.Location = new Point(366, 304);
//        labelParamMin5.Name = "labelParamMin5";
//        labelParamMin5.Size = new Size(60, 16);
//        labelParamMin5.TabIndex = 25;
//        labelParamMin5.Text = "min";
//        labelParamMin5.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // labelParamMax5
//        // 
//        labelParamMax5.Location = new Point(627, 312);
//        labelParamMax5.Name = "labelParamMax5";
//        labelParamMax5.Size = new Size(52, 13);
//        labelParamMax5.TabIndex = 26;
//        labelParamMax5.Text = "max";
//        labelParamMax5.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // labelParamName6
//        // 
//        labelParamName6.Location = new Point(169, 352);
//        labelParamName6.Name = "labelParamName6";
//        labelParamName6.Size = new Size(117, 13);
//        labelParamName6.TabIndex = 27;
//        labelParamName6.TextAlign = ContentAlignment.TopRight;
//        // 
//        // labelParamValue6
//        // 
//        labelParamValue6.BorderStyle = BorderStyle.Fixed3D;
//        labelParamValue6.Location = new Point(294, 352);
//        labelParamValue6.Name = "labelParamValue6";
//        labelParamValue6.Size = new Size(67, 16);
//        labelParamValue6.TabIndex = 28;
//        labelParamValue6.Text = "Value";
//        labelParamValue6.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // trackbarSlider6
//        // 
//        trackbarSlider6.Location = new Point(426, 352);
//        trackbarSlider6.Name = "trackbarSlider6";
//        trackbarSlider6.Size = new Size(195, 45);
//        trackbarSlider6.TabIndex = 29;
//        trackbarSlider6.Text = "Slider1";
//        trackbarSlider6.TickStyle = TickStyle.None;
//        trackbarSlider6.Scroll += new EventHandler(trackbarSliderScroll);
//        // 
//        // labelParamMin6
//        // 
//        labelParamMin6.Location = new Point(366, 352);
//        labelParamMin6.Name = "labelParamMin6";
//        labelParamMin6.Size = new Size(60, 16);
//        labelParamMin6.TabIndex = 30;
//        labelParamMin6.Text = "min";
//        labelParamMin6.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // labelParamMax6
//        // 
//        labelParamMax6.Location = new Point(627, 352);
//        labelParamMax6.Name = "labelParamMax6";
//        labelParamMax6.Size = new Size(52, 13);
//        labelParamMax6.TabIndex = 31;
//        labelParamMax6.Text = "max";
//        labelParamMax6.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // radiobuttonTriangle
//        // 
//        radiobuttonTriangle.Location = new Point(16, 16);
//        radiobuttonTriangle.Name = "radiobuttonTriangle";
//        radiobuttonTriangle.Size = new Size(69, 16);
//        radiobuttonTriangle.TabIndex = 32;
//        radiobuttonTriangle.Text = "Triangle";
//        radiobuttonTriangle.CheckedChanged += new EventHandler(trackbarSliderScroll);
//        // 
//        // radiobuttonSquare
//        // 
//        radiobuttonSquare.Location = new Point(88, 16);
//        radiobuttonSquare.Name = "radiobuttonSquare";
//        radiobuttonSquare.Size = new Size(64, 16);
//        radiobuttonSquare.TabIndex = 33;
//        radiobuttonSquare.Text = "Square";
//        radiobuttonSquare.CheckedChanged += new EventHandler(trackbarSliderScroll);
//        // 
//        // radiobuttonRadioSine
//        // 
//        radiobuttonRadioSine.Location = new Point(152, 16);
//        radiobuttonRadioSine.Name = "radiobuttonRadioSine";
//        radiobuttonRadioSine.Size = new Size(48, 16);
//        radiobuttonRadioSine.TabIndex = 34;
//        radiobuttonRadioSine.Text = "Sine";
//        radiobuttonRadioSine.CheckedChanged += new EventHandler(trackbarSliderScroll);
//        // 
//        // groupboxFrameWaveform
//        // 
//        groupboxFrameWaveform.Controls.AddRange(new Control[]
//                                                    {
//                                                        radiobuttonSquare,
//                                                        radiobuttonTriangle,
//                                                        radiobuttonRadioSine
//                                                    });
//        groupboxFrameWaveform.Location = new Point(180, 400);
//        groupboxFrameWaveform.Name = "groupboxFrameWaveform";
//        groupboxFrameWaveform.Size = new Size(225, 42);
//        groupboxFrameWaveform.TabIndex = 35;
//        groupboxFrameWaveform.TabStop = false;
//        groupboxFrameWaveform.Text = "Waveform";
//        // 
//        // buttonOpen
//        // 
//        buttonOpen.Location = new Point(12, 12);
//        buttonOpen.Name = "buttonOpen";
//        buttonOpen.TabIndex = 47;
//        buttonOpen.Text = "&Open File";
//        buttonOpen.Click += new EventHandler(buttonOpen_Click);
//        // 
//        // labelTextFilename
//        // 
//        labelTextFilename.BorderStyle = BorderStyle.Fixed3D;
//        labelTextFilename.Location = new Point(94, 14);
//        labelTextFilename.Name = "labelTextFilename";
//        labelTextFilename.Size = new Size(595, 20);
//        labelTextFilename.TabIndex = 48;
//        labelTextFilename.Text = "Filename";
//        labelTextFilename.TextAlign = ContentAlignment.MiddleLeft;
//        // 
//        // labelStatic2
//        // 
//        labelStatic2.Location = new Point(19, 44);
//        labelStatic2.Name = "labelStatic2";
//        labelStatic2.Size = new Size(67, 16);
//        labelStatic2.TabIndex = 49;
//        labelStatic2.Text = "Status";
//        // 
//        // labelTextStatus
//        // 
//        labelTextStatus.BorderStyle = BorderStyle.Fixed3D;
//        labelTextStatus.Location = new Point(94, 44);
//        labelTextStatus.Name = "labelTextStatus";
//        labelTextStatus.Size = new Size(595, 20);
//        labelTextStatus.TabIndex = 50;
//        labelTextStatus.Text = "No file loaded.";
//        labelTextStatus.TextAlign = ContentAlignment.MiddleLeft;
//        // 
//        // checkboxLoop
//        // 
//        checkboxLoop.Location = new Point(42, 376);
//        checkboxLoop.Name = "checkboxLoop";
//        checkboxLoop.Size = new Size(86, 16);
//        checkboxLoop.TabIndex = 51;
//        checkboxLoop.Text = "&Loop Sound";
//        checkboxLoop.CheckedChanged += new EventHandler(checkboxLoop_CheckedChanged);
//        // 
//        // buttonPlay
//        // 
//        buttonPlay.Location = new Point(10, 400);
//        buttonPlay.Name = "buttonPlay";
//        buttonPlay.Size = new Size(67, 23);
//        buttonPlay.TabIndex = 52;
//        buttonPlay.Text = "&Play";
//        buttonPlay.Click += new EventHandler(buttonPlay_Click);
//        // 
//        // buttonStop
//        // 
//        buttonStop.Location = new Point(87, 400);
//        buttonStop.Name = "buttonStop";
//        buttonStop.Size = new Size(67, 23);
//        buttonStop.TabIndex = 53;
//        buttonStop.Text = "&Stop";
//        buttonStop.Enabled = false;
//        buttonStop.Click += new EventHandler(buttonStop_Click);
//        // 
//        // labelStatic3
//        // 
//        labelStatic3.BorderStyle = BorderStyle.Fixed3D;
//        labelStatic3.Location = new Point(372, 88);
//        labelStatic3.Name = "labelStatic3";
//        labelStatic3.Size = new Size(52, 16);
//        labelStatic3.TabIndex = 62;
//        labelStatic3.Text = "Min";
//        labelStatic3.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // labelStatic4
//        // 
//        labelStatic4.BorderStyle = BorderStyle.Fixed3D;
//        labelStatic4.Location = new Point(627, 88);
//        labelStatic4.Name = "labelStatic4";
//        labelStatic4.Size = new Size(52, 16);
//        labelStatic4.TabIndex = 64;
//        labelStatic4.Text = "Max";
//        labelStatic4.TextAlign = ContentAlignment.TopCenter;
//        // 
//        // radiobuttonRadioNeg180
//        // 
//        radiobuttonRadioNeg180.Location = new Point(16, 16);
//        radiobuttonRadioNeg180.Name = "radiobuttonRadioNeg180";
//        radiobuttonRadioNeg180.Size = new Size(45, 16);
//        radiobuttonRadioNeg180.TabIndex = 65;
//        radiobuttonRadioNeg180.Text = "-180";
//        radiobuttonRadioNeg180.CheckedChanged += new EventHandler(trackbarSliderScroll);
//        // 
//        // radiobuttonRadioNeg90
//        // 
//        radiobuttonRadioNeg90.Location = new Point(72, 16);
//        radiobuttonRadioNeg90.Name = "radiobuttonRadioNeg90";
//        radiobuttonRadioNeg90.Size = new Size(39, 16);
//        radiobuttonRadioNeg90.TabIndex = 66;
//        radiobuttonRadioNeg90.Text = "-90";
//        radiobuttonRadioNeg90.CheckedChanged += new EventHandler(trackbarSliderScroll);
//        // 
//        // radiobuttonRadioZero
//        // 
//        radiobuttonRadioZero.Location = new Point(120, 16);
//        radiobuttonRadioZero.Name = "radiobuttonRadioZero";
//        radiobuttonRadioZero.Size = new Size(30, 16);
//        radiobuttonRadioZero.TabIndex = 67;
//        radiobuttonRadioZero.Text = "0";
//        radiobuttonRadioZero.CheckedChanged += new EventHandler(trackbarSliderScroll);
//        // 
//        // radiobuttonRadio90
//        // 
//        radiobuttonRadio90.Location = new Point(152, 16);
//        radiobuttonRadio90.Name = "radiobuttonRadio90";
//        radiobuttonRadio90.Size = new Size(36, 16);
//        radiobuttonRadio90.TabIndex = 68;
//        radiobuttonRadio90.Text = "90";
//        radiobuttonRadio90.CheckedChanged += new EventHandler(trackbarSliderScroll);
//        // 
//        // radiobuttonRadio180
//        // 
//        radiobuttonRadio180.Location = new Point(200, 16);
//        radiobuttonRadio180.Name = "radiobuttonRadio180";
//        radiobuttonRadio180.Size = new Size(42, 16);
//        radiobuttonRadio180.TabIndex = 69;
//        radiobuttonRadio180.Text = "180";
//        radiobuttonRadio180.CheckedChanged += new EventHandler(trackbarSliderScroll);
//        // 
//        // groupboxFramePhase
//        // 
//        groupboxFramePhase.Controls.AddRange(new Control[]
//                                                 {
//                                                     radiobuttonRadioNeg180,
//                                                     radiobuttonRadioNeg90,
//                                                     radiobuttonRadioZero,
//                                                     radiobuttonRadio90,
//                                                     radiobuttonRadio180
//                                                 });
//        groupboxFramePhase.Location = new Point(420, 400);
//        groupboxFramePhase.Name = "groupboxFramePhase";
//        groupboxFramePhase.Size = new Size(247, 42);
//        groupboxFramePhase.TabIndex = 63;
//        groupboxFramePhase.TabStop = false;
//        groupboxFramePhase.Text = "Phase (Degrees)";
//        // 
//        // groupboxEffects
//        // 
//        groupboxEffects.Controls.AddRange(new Control[]
//                                              {
//                                                  buttonDelete,
//                                                  listboxEffects,
//                                                  comboEffects
//                                              });
//        groupboxEffects.Location = new Point(8, 80);
//        groupboxEffects.Name = "groupboxEffects";
//        groupboxEffects.Size = new Size(144, 280);
//        groupboxEffects.TabIndex = 71;
//        groupboxEffects.TabStop = false;
//        groupboxEffects.Text = "Effects";
//        // 
//        // buttonDelete
//        // 
//        buttonDelete.Location = new Point(40, 248);
//        buttonDelete.Name = "buttonDelete";
//        buttonDelete.Size = new Size(64, 24);
//        buttonDelete.TabIndex = 3;
//        buttonDelete.Text = "Delete";
//        buttonDelete.Click += new EventHandler(buttonDelete_Click);
//        // 
//        // listboxEffects
//        // 
//        listboxEffects.Location = new Point(8, 48);
//        listboxEffects.Name = "listboxEffects";
//        listboxEffects.Size = new Size(128, 186);
//        listboxEffects.TabIndex = 2;
//        listboxEffects.KeyUp += new KeyEventHandler(listboxEffects_KeyUp);
//        listboxEffects.SelectedIndexChanged += new EventHandler(listboxEffects_SelectedIndexChanged);
//        // 
//        // comboEffects
//        // 
//        comboEffects.DropDownStyle = ComboBoxStyle.DropDownList;
//        comboEffects.Items.AddRange(new object[]
//                                        {
//                                            "Chorus",
//                                            "Compressor",
//                                            "Distortion",
//                                            "Echo",
//                                            "Flanger",
//                                            "Gargle",
//                                            "Waves Reverb",
//                                            "ParamEq"
//                                        });
//        comboEffects.Location = new Point(8, 16);
//        comboEffects.Name = "comboEffects";
//        comboEffects.Size = new Size(128, 21);
//        comboEffects.TabIndex = 1;
//        comboEffects.SelectedValueChanged += new EventHandler(comboEffects_SelectedValueChanged);
//        // 
//        // timer1
//        // 
//        timer1.Interval = 50;
//        timer1.Tick += new EventHandler(timer1_Tick);
//        // 
//        // MainForm
//        // 
//        AcceptButton = buttonOk;
//        ClientSize = new Size(700, 472);
//        Controls.AddRange(new Control[]
//                              {
//                                  groupboxEffects,
//                                  buttonOk,
//                                  labelParamName1,
//                                  labelParamValue1,
//                                  trackbarSlider1,
//                                  labelParamMin1,
//                                  labelParamMax1,
//                                  labelParamName2,
//                                  labelParamValue2,
//                                  trackbarSlider2,
//                                  labelParamMin2,
//                                  labelParamMax2,
//                                  labelParamName3,
//                                  labelParamValue3,
//                                  trackbarSlider3,
//                                  labelParamMin3,
//                                  labelParamMax3,
//                                  labelParamName4,
//                                  labelParamValue4,
//                                  trackbarSlider4,
//                                  labelParamMin4,
//                                  labelParamMax4,
//                                  labelParamName5,
//                                  labelParamValue5,
//                                  trackbarSlider5,
//                                  labelParamMin5,
//                                  labelParamMax5,
//                                  labelParamName6,
//                                  labelParamValue6,
//                                  trackbarSlider6,
//                                  labelParamMin6,
//                                  labelParamMax6,
//                                  buttonOpen,
//                                  labelTextFilename,
//                                  labelStatic2,
//                                  labelTextStatus,
//                                  checkboxLoop,
//                                  buttonPlay,
//                                  buttonStop,
//                                  labelStatic3,
//                                  labelStatic4,
//                                  groupboxFrameWaveform,
//                                  groupboxFramePhase,
//                                  groupboxFrame
//                              });
//        FormBorderStyle = FormBorderStyle.FixedSingle;
//        Location = new Point(150, 160);
//        MaximizeBox = false;
//        Name = "MainForm";
//        Text = "SoundFX - Sound effects applied to Device.SecondaryBuffer";
//        Closing += new CancelEventHandler(MainForm_Closing);
//        (trackbarSlider1).EndInit();
//        (trackbarSlider2).EndInit();
//        (trackbarSlider3).EndInit();
//        (trackbarSlider4).EndInit();
//        (trackbarSlider5).EndInit();
//        (trackbarSlider6).EndInit();
//        groupboxFrameWaveform.ResumeLayout(false);
//        groupboxFramePhase.ResumeLayout(false);
//        groupboxEffects.ResumeLayout(false);
//        ResumeLayout(false);
//    }

//    #endregion

//    private void InitDirectSound()
//    {
//        try
//        {
//            applicationDevice = new Device();
//            applicationDevice.SetCooperativeLevel(this, CooperativeLevel.Priority);
//        }
//        catch
//        {
//            MessageBox.Show("Unable to create sound device. Sample will now exit.");
//            Close();
//            throw;
//        }
//    }

//    protected override void Dispose(bool disposing)
//    {
//        if (disposing)
//        {
//            if (applicationBuffer != null)
//                applicationBuffer.Dispose();
//            if (applicationDevice != null)
//                applicationDevice.Dispose();
//        }
//        base.Dispose(disposing);
//    }

//    private void buttonOpen_Click(object sender, EventArgs e)
//    {
//        BufferDescription description = new BufferDescription();
//        OpenFileDialog ofd = new OpenFileDialog();

//        labelTextStatus.Text = "Loading file...";
//        // Get the default media path (something like C:\WINDOWS\MEDIA)
//        if (string.Empty == path)
//            path = Environment.SystemDirectory.Substring(0, Environment.SystemDirectory.LastIndexOf("\\")) + "\\media";

//        ofd.DefaultExt = ".wav";
//        ofd.Filter = "Wave Files|*.wav|All Files|*.*";
//        ofd.FileName = fileName;
//        ofd.InitialDirectory = path;

//        if (null != applicationBuffer)
//        {
//            applicationBuffer.Stop();
//            applicationBuffer.SetCurrentPosition(0);
//        }

//        // Display the OpenFileName dialog. Then, try to load the specified file
//        if (DialogResult.Cancel == ofd.ShowDialog(this))
//        {
//            if (null != applicationBuffer)
//                applicationBuffer.Dispose();

//            labelTextStatus.Text = "No file loaded.";
//            return;
//        }
//        fileName = string.Empty;

//        description.ControlEffects = true;

//        // Create a SecondaryBuffer using the file name.
//        try
//        {
//            applicationBuffer = new SecondaryBuffer(ofd.FileName, description, applicationDevice);
//        }
//        catch (BufferTooSmallException)
//        {
//            labelTextStatus.Text = "Wave file is too small to be used with effects.";
//            return;
//        }
//        catch (FormatException)
//        {
//            // Invalid file was used. Managed DirectSound tries to convert any files less than
//            // 8 bit to 8 bit. Some drivers don't support this conversion, so make sure to
//            // catch the FormatException if it's thrown.
//            labelTextStatus.Text = "Failed to create SecondaryBuffer from selected file.";
//            return;
//        }

//        // Remember the file for next time
//        if (null != applicationBuffer)
//        {
//            fileName = ofd.FileName;
//            path = fileName.Substring(0, fileName.LastIndexOf("\\"));
//        }
//        labelTextFilename.Text = fileName;
//        labelTextStatus.Text = "File loaded.";
//    }

//    private void buttonPlay_Click(object sender, EventArgs e)
//    {
//        if (null != applicationBuffer)
//        {
//            applicationBuffer.Play(0, (shouldLoop) ? BufferPlayFlags.Looping : BufferPlayFlags.Default);
//            timer1.Enabled = true;
//            buttonStop.Enabled = true;
//            buttonPlay.Enabled = false;
//            comboEffects.Enabled = false;
//            labelTextStatus.Text = "Sound playing.";
//        }
//    }

//    private void buttonStop_Click(object sender, EventArgs e)
//    {
//        if (null != applicationBuffer)
//            if (applicationBuffer.Status.Playing)
//            {
//                applicationBuffer.Stop();
//                applicationBuffer.SetCurrentPosition(0);
//                timer1.Enabled = false;
//                buttonStop.Enabled = false;
//                buttonPlay.Enabled = true;
//                comboEffects.Enabled = true;
//                labelTextStatus.Text = "Sound stopped.";
//            }
//    }

//    private void comboEffects_SelectedValueChanged(object sender, EventArgs e)
//    {
//        string description = string.Empty;

//        if (null == applicationBuffer)
//            return;

//        EffectInfo[] temp = new EffectInfo[effectDescription.Count + 1];
//        effectDescription.CopyTo(temp, 0);

//        switch (comboEffects.SelectedIndex)
//        {
//            case 0:
//                temp[temp.Length - 1].description.GuidEffectClass = DSoundHelper.StandardChorusGuid;
//                description = "Chorus";
//                break;
//            case 1:
//                temp[temp.Length - 1].description.GuidEffectClass = DSoundHelper.StandardCompressorGuid;
//                description = "Compressor";
//                break;
//            case 2:
//                temp[temp.Length - 1].description.GuidEffectClass = DSoundHelper.StandardDistortionGuid;
//                description = "Distortion";
//                break;
//            case 3:
//                temp[temp.Length - 1].description.GuidEffectClass = DSoundHelper.StandardEchoGuid;
//                description = "Echo";
//                break;
//            case 4:
//                temp[temp.Length - 1].description.GuidEffectClass = DSoundHelper.StandardFlangerGuid;
//                description = "Flanger";
//                break;
//            case 5:
//                temp[temp.Length - 1].description.GuidEffectClass = DSoundHelper.StandardGargleGuid;
//                description = "Gargle";
//                break;
//            case 6:
//                temp[temp.Length - 1].description.GuidEffectClass = DSoundHelper.StandardWavesReverbGuid;
//                description = "Waves Reverb";
//                break;
//            case 7:
//                temp[temp.Length - 1].description.GuidEffectClass = DSoundHelper.StandardParamEqGuid;
//                description = "ParamEq";
//                break;
//        }

//        if (AddEffect(temp))
//        {
//            effectDescription.Clear();
//            effectDescription.AddRange(temp);
//            listboxEffects.Items.Add(description);
//            listboxEffects.SelectedIndex = listboxEffects.Items.Count - 1;
//        }
//    }

//    private bool AddEffect(EffectInfo[] temp)
//    {
//        EffectsReturnValue[] ret;
//        EffectDescription[] fx = null;
//        bool WasPlaying = false;
//        int count = 0;

//        if (null != temp)
//        {
//            fx = new EffectDescription[temp.Length];
//            count = temp.Length;
//        }

//        if (applicationBuffer.Status.Playing)
//            WasPlaying = true;

//        applicationBuffer.Stop();

//        // Store the current params for each effect.
//        for (int i = 0; i < count; i++)
//            fx[i] = (temp[i]).description;

//        try
//        {
//            ret = applicationBuffer.SetEffects(fx);
//        }
//        catch (DirectXException)
//        {
//            labelTextStatus.Text = "Unable to set effect on the buffer. Some effects can't be set on 8 bit wave files.";

//            // Revert to the last valid effects.
//            if (temp.Length <= 1)
//                return false;

//            fx = new EffectDescription[temp.Length - 1];
//            ;
//            for (int i = 0; i < count - 1; i++)
//                fx[i] = (temp[i]).description;

//            try
//            {
//                applicationBuffer.SetEffects(fx);
//            }
//            catch (DirectXException)
//            {
//            }

//            return false;
//        }

//        // Restore the params for each effect.
//        for (int i = 0; i < count; i++)
//        {
//            EffectInfo eff = new EffectInfo();

//            eff.Effect = applicationBuffer.GetEffects(i);
//            eff.EffectSettings = temp[i].EffectSettings;
//            eff.description = temp[i].description;

//            Type efftype = eff.Effect.GetType();

//            if (typeof(ChorusEffect) == efftype)
//                if (null != eff.EffectSettings)
//                    ((ChorusEffect)eff.Effect).AllParameters = (EffectsChorus)eff.EffectSettings;
//                else
//                    eff.EffectSettings = ((ChorusEffect)eff.Effect).AllParameters;
//            else if (typeof(CompressorEffect) == efftype)
//                if (null != eff.EffectSettings)
//                    ((CompressorEffect)eff.Effect).AllParameters = (EffectsCompressor)eff.EffectSettings;
//                else
//                    eff.EffectSettings = ((CompressorEffect)eff.Effect).AllParameters;
//            else if (typeof(DistortionEffect) == efftype)
//                if (null != eff.EffectSettings)
//                    ((DistortionEffect)eff.Effect).AllParameters = (EffectsDistortion)eff.EffectSettings;
//                else
//                    eff.EffectSettings = ((DistortionEffect)eff.Effect).AllParameters;
//            else if (typeof(EchoEffect) == efftype)
//                if (null != eff.EffectSettings)
//                    ((EchoEffect)eff.Effect).AllParameters = (EffectsEcho)eff.EffectSettings;
//                else
//                    eff.EffectSettings = ((EchoEffect)eff.Effect).AllParameters;
//            else if (typeof(FlangerEffect) == efftype)
//                if (null != eff.EffectSettings)
//                    ((FlangerEffect)eff.Effect).AllParameters = (EffectsFlanger)eff.EffectSettings;
//                else
//                    eff.EffectSettings = ((FlangerEffect)eff.Effect).AllParameters;
//            else if (typeof(GargleEffect) == efftype)
//                if (null != eff.EffectSettings)
//                    ((GargleEffect)eff.Effect).AllParameters = (EffectsGargle)eff.EffectSettings;
//                else
//                    eff.EffectSettings = ((GargleEffect)eff.Effect).AllParameters;
//            else if (typeof(ParamEqEffect) == efftype)
//                if (null != eff.EffectSettings)
//                    ((ParamEqEffect)eff.Effect).AllParameters = (EffectsParamEq)eff.EffectSettings;
//                else
//                    eff.EffectSettings = ((ParamEqEffect)eff.Effect).AllParameters;
//            else if (typeof(WavesReverbEffect) == efftype)
//                if (null != eff.EffectSettings)
//                    ((WavesReverbEffect)eff.Effect).AllParameters = (EffectsWavesReverb)eff.EffectSettings;
//                else
//                    eff.EffectSettings = ((WavesReverbEffect)eff.Effect).AllParameters;

//            temp[i] = eff;
//        }

//        if (WasPlaying)
//            applicationBuffer.Play(0, (shouldLoop) ? BufferPlayFlags.Looping : BufferPlayFlags.Default);

//        if (null != temp)
//            if ((EffectsReturnValue.LocatedInHardware == ret[temp.Length - 1]) ||
//                (EffectsReturnValue.LocatedInSoftware == ret[temp.Length - 1]))
//                return true;

//        return false;
//    }

//    private void checkboxLoop_CheckedChanged(object sender, EventArgs e)
//    {
//        shouldLoop = checkboxLoop.Checked;
//    }

//    private void listboxEffects_SelectedIndexChanged(object sender, EventArgs e)
//    {
//        if (-1 == listboxEffects.SelectedIndex)
//            return;

//        currentIndex = listboxEffects.SelectedIndex;

//        // Make sure we don't update any settings while updating the UI
//        isIgnoringSettings = true;
//        UpdateUI(true);
//        isIgnoringSettings = false;
//    }

//    private void UpdateUI(bool MoveControls)
//    {
//        ClearUI(MoveControls);

//        Type efftype = ((EffectInfo)effectDescription[currentIndex]).Effect.GetType();
//        object eff = ((EffectInfo)effectDescription[currentIndex]).Effect;

//        if (typeof(ChorusEffect) == efftype)
//        {
//            EffectsChorus temp = ((ChorusEffect)eff).AllParameters;

//            if (MoveControls)
//            {
//                trackbarSlider1.Minimum = (int)ChorusEffect.WetDryMixMin;
//                trackbarSlider1.Maximum = (int)ChorusEffect.WetDryMixMax;
//                trackbarSlider1.Value = (int)temp.WetDryMix;
//                trackbarSlider2.Minimum = (int)ChorusEffect.DepthMin;
//                trackbarSlider2.Maximum = (int)ChorusEffect.DepthMax;
//                trackbarSlider2.Value = (int)temp.Depth;
//                trackbarSlider3.Minimum = (int)ChorusEffect.FeedbackMin;
//                trackbarSlider3.Maximum = (int)ChorusEffect.FeedbackMax;
//                trackbarSlider3.Value = (int)temp.Feedback;
//                trackbarSlider4.Minimum = (int)ChorusEffect.FrequencyMin;
//                trackbarSlider4.Maximum = (int)ChorusEffect.FrequencyMax;
//                trackbarSlider4.Value = (int)temp.Frequency;
//                trackbarSlider5.Minimum = (int)ChorusEffect.DelayMin;
//                trackbarSlider5.Maximum = (int)ChorusEffect.DelayMax;
//                trackbarSlider5.Value = (int)temp.Delay;

//                if (ChorusEffect.WaveSin == temp.Waveform)
//                    radiobuttonRadioSine.Checked = true;
//                else
//                    radiobuttonTriangle.Checked = true;

//                if (ChorusEffect.PhaseNegative180 == temp.Phase)
//                    radiobuttonRadioNeg180.Checked = true;
//                else if (ChorusEffect.PhaseNegative90 == temp.Phase)
//                    radiobuttonRadioNeg90.Checked = true;
//                else if (ChorusEffect.PhaseZero == temp.Phase)
//                    radiobuttonRadioZero.Checked = true;
//                else if (ChorusEffect.Phase90 == temp.Phase)
//                    radiobuttonRadio90.Checked = true;
//                else if (ChorusEffect.Phase180 == temp.Phase)
//                    radiobuttonRadio180.Checked = true;

//                groupboxFramePhase.Enabled = radiobuttonRadioNeg180.Enabled = radiobuttonRadioNeg90.Enabled =
//                                                                              radiobuttonRadioZero.Enabled =
//                                                                              radiobuttonRadio90.Enabled =
//                                                                              radiobuttonRadio180.Enabled =
//                                                                              groupboxFrameWaveform.Enabled =
//                                                                              radiobuttonRadioSine.Enabled =
//                                                                              radiobuttonTriangle.Enabled = true;

//                trackbarSlider1.Enabled = trackbarSlider2.Enabled = trackbarSlider3.Enabled =
//                                                                    trackbarSlider4.Enabled =
//                                                                    trackbarSlider5.Enabled = true;
//            }
//            labelParamValue1.Text = temp.WetDryMix.ToString();
//            labelParamName1.Text = "Wet/Dry Mix (%)";

//            labelParamValue2.Text = temp.Depth.ToString();
//            labelParamName2.Text = "Depth (%)";

//            labelParamValue3.Text = temp.Feedback.ToString();
//            labelParamName3.Text = "Feedback (%)";

//            labelParamValue4.Text = temp.Frequency.ToString();
//            labelParamName4.Text = "Frequency (Hz)";

//            labelParamValue5.Text = temp.Delay.ToString();
//            labelParamName5.Text = "Delay (ms)";
//        }
//        else if (typeof(CompressorEffect) == efftype)
//        {
//            EffectsCompressor temp = ((CompressorEffect)eff).AllParameters;

//            if (MoveControls)
//            {
//                trackbarSlider1.Minimum = (int)CompressorEffect.GainMin;
//                trackbarSlider1.Maximum = (int)CompressorEffect.GainMax;
//                trackbarSlider1.Value = (int)temp.Gain;
//                trackbarSlider2.Minimum = (int)CompressorEffect.AttackMin;
//                trackbarSlider2.Maximum = (int)CompressorEffect.AttackMax;
//                trackbarSlider2.Value = (int)temp.Attack;
//                trackbarSlider3.Minimum = (int)CompressorEffect.ReleaseMin;
//                trackbarSlider3.Maximum = (int)CompressorEffect.ReleaseMax;
//                trackbarSlider3.Value = (int)temp.Release;
//                trackbarSlider4.Minimum = (int)CompressorEffect.ThresholdMin;
//                trackbarSlider4.Maximum = (int)CompressorEffect.ThresholdMax;
//                trackbarSlider4.Value = (int)temp.Threshold;
//                trackbarSlider5.Minimum = (int)CompressorEffect.RatioMin;
//                trackbarSlider5.Maximum = (int)CompressorEffect.RatioMax;
//                trackbarSlider5.Value = (int)temp.Ratio;
//                trackbarSlider6.Minimum = (int)CompressorEffect.PreDelayMin;
//                trackbarSlider6.Maximum = (int)CompressorEffect.PreDelayMax;
//                trackbarSlider6.Value = (int)temp.Predelay;

//                trackbarSlider1.Enabled = trackbarSlider2.Enabled = trackbarSlider3.Enabled =
//                                                                    trackbarSlider4.Enabled =
//                                                                    trackbarSlider5.Enabled =
//                                                                    trackbarSlider6.Enabled = true;
//            }
//            labelParamValue1.Text = temp.Gain.ToString();
//            labelParamName1.Text = "Gain (dB)";

//            labelParamName2.Text = "Attack (ms)";
//            labelParamValue2.Text = temp.Attack.ToString();

//            labelParamName3.Text = "Release (ms)";
//            labelParamValue3.Text = temp.Release.ToString();

//            labelParamName4.Text = "Threshold (dB)";
//            labelParamValue4.Text = temp.Threshold.ToString();

//            labelParamName5.Text = "Ratio (x:1)";
//            labelParamValue5.Text = temp.Ratio.ToString();

//            labelParamName6.Text = "Predelay (ms)";
//            labelParamValue6.Text = temp.Predelay.ToString();
//        }
//        else if (typeof(DistortionEffect) == efftype)
//        {
//            EffectsDistortion temp = ((DistortionEffect)eff).AllParameters;

//            if (MoveControls)
//            {
//                trackbarSlider1.Minimum = (int)DistortionEffect.GainMin;
//                trackbarSlider1.Maximum = (int)DistortionEffect.GainMax;
//                trackbarSlider1.Value = (int)temp.Gain;
//                trackbarSlider2.Minimum = (int)DistortionEffect.EdgeMin;
//                trackbarSlider2.Maximum = (int)DistortionEffect.EdgeMax;
//                trackbarSlider2.Value = (int)temp.Edge;
//                trackbarSlider3.Minimum = (int)DistortionEffect.PostEqCenterFrequencyMin;
//                trackbarSlider3.Maximum = (int)DistortionEffect.PostEqCenterFrequencyMax;
//                trackbarSlider3.Value = (int)temp.PostEqCenterFrequency;
//                trackbarSlider4.Minimum = (int)DistortionEffect.PostEqBandwidthMin;
//                trackbarSlider4.Maximum = (int)DistortionEffect.PostEqBandwidthMax;
//                trackbarSlider4.Value = (int)temp.PostEqBandwidth;
//                trackbarSlider5.Minimum = (int)DistortionEffect.PreLowPassCutoffMin;
//                trackbarSlider5.Maximum = (int)DistortionEffect.PreLowPassCutoffMax;
//                trackbarSlider5.Value = (int)temp.PreLowpassCutoff;

//                trackbarSlider1.Enabled = trackbarSlider2.Enabled = trackbarSlider3.Enabled =
//                                                                    trackbarSlider4.Enabled =
//                                                                    trackbarSlider5.Enabled = true;
//            }
//            labelParamName1.Text = "Gain (dB)";
//            labelParamValue1.Text = temp.Gain.ToString();

//            labelParamName2.Text = "Edge (%)";
//            labelParamValue2.Text = temp.Edge.ToString();

//            labelParamName3.Text = "PostEQ Center Freq (Hz)";
//            labelParamValue3.Text = temp.PostEqCenterFrequency.ToString();

//            labelParamName4.Text = "PostEQ Bandwidth (Hz)";
//            labelParamValue4.Text = temp.PostEqBandwidth.ToString();

//            labelParamName5.Text = "PreLowpass Cutoff (Hz)";
//            labelParamValue5.Text = temp.PreLowpassCutoff.ToString();
//        }
//        else if (typeof(EchoEffect) == efftype)
//        {
//            EffectsEcho temp = ((EchoEffect)eff).AllParameters;

//            if (MoveControls)
//            {
//                trackbarSlider1.Minimum = (int)EchoEffect.WetDryMixMin;
//                trackbarSlider1.Maximum = (int)EchoEffect.WetDryMixMax;
//                trackbarSlider1.Value = (int)temp.WetDryMix;
//                trackbarSlider2.Minimum = (int)EchoEffect.FeedbackMin;
//                trackbarSlider2.Maximum = (int)EchoEffect.FeedbackMax;
//                trackbarSlider2.Value = (int)temp.Feedback;
//                trackbarSlider3.Minimum = (int)EchoEffect.LeftDelayMin;
//                trackbarSlider3.Maximum = (int)EchoEffect.LeftDelayMax;
//                trackbarSlider3.Value = (int)temp.LeftDelay;
//                trackbarSlider4.Minimum = (int)EchoEffect.RightDelayMin;
//                trackbarSlider4.Maximum = (int)EchoEffect.RightDelayMax;
//                trackbarSlider4.Value = (int)temp.RightDelay;
//                trackbarSlider5.Minimum = EchoEffect.PanDelayMin;
//                trackbarSlider5.Maximum = EchoEffect.PanDelayMax;
//                trackbarSlider5.Value = temp.PanDelay;

//                trackbarSlider1.Enabled = trackbarSlider2.Enabled = trackbarSlider3.Enabled =
//                                                                    trackbarSlider4.Enabled =
//                                                                    trackbarSlider5.Enabled = true;
//            }
//            labelParamName1.Text = "Wet/Dry Mix (%)";
//            labelParamValue1.Text = temp.WetDryMix.ToString();

//            labelParamName2.Text = "Feedback (%)";
//            labelParamValue2.Text = temp.Feedback.ToString();

//            labelParamName3.Text = "Left Delay (ms)";
//            labelParamValue3.Text = temp.LeftDelay.ToString();

//            labelParamName4.Text = "Right Delay (ms)";
//            labelParamValue4.Text = temp.RightDelay.ToString();

//            labelParamName5.Text = "Pan Delay (bool)";
//            labelParamValue5.Text = temp.PanDelay.ToString();
//        }
//        else if (typeof(FlangerEffect) == efftype)
//        {
//            EffectsFlanger temp = ((FlangerEffect)eff).AllParameters;

//            if (MoveControls)
//            {
//                trackbarSlider1.Minimum = (int)FlangerEffect.WetDryMixMin;
//                trackbarSlider1.Maximum = (int)FlangerEffect.WetDryMixMax;
//                trackbarSlider1.Value = (int)temp.WetDryMix;
//                trackbarSlider2.Minimum = (int)FlangerEffect.DepthMin;
//                trackbarSlider2.Maximum = (int)FlangerEffect.DepthMax;
//                trackbarSlider2.Value = (int)temp.Depth;
//                trackbarSlider3.Minimum = (int)FlangerEffect.FeedbackMin;
//                trackbarSlider3.Maximum = (int)FlangerEffect.FeedbackMax;
//                trackbarSlider3.Value = (int)temp.Feedback;
//                trackbarSlider4.Minimum = (int)FlangerEffect.FrequencyMin;
//                trackbarSlider4.Maximum = (int)FlangerEffect.FrequencyMax;
//                trackbarSlider4.Value = (int)temp.Frequency;
//                trackbarSlider5.Minimum = (int)FlangerEffect.DelayMin;
//                trackbarSlider5.Maximum = (int)FlangerEffect.DelayMax;
//                trackbarSlider5.Value = (int)temp.Delay;

//                if (ChorusEffect.WaveSin == temp.Waveform)
//                    radiobuttonRadioSine.Checked = true;
//                else
//                    radiobuttonTriangle.Checked = true;

//                if (FlangerEffect.PhaseNeg180 == temp.Phase)
//                    radiobuttonRadioNeg180.Checked = true;
//                else if (FlangerEffect.PhaseNeg90 == temp.Phase)
//                    radiobuttonRadioNeg90.Checked = true;
//                else if (FlangerEffect.PhaseZero == temp.Phase)
//                    radiobuttonRadioZero.Checked = true;
//                else if (FlangerEffect.Phase90 == temp.Phase)
//                    radiobuttonRadio90.Checked = true;
//                else if (FlangerEffect.Phase180 == temp.Phase)
//                    radiobuttonRadio180.Checked = true;

//                groupboxFramePhase.Enabled = radiobuttonRadioNeg180.Enabled = radiobuttonRadioNeg90.Enabled =
//                                                                              radiobuttonRadioZero.Enabled =
//                                                                              radiobuttonRadio90.Enabled =
//                                                                              radiobuttonRadio180.Enabled =
//                                                                              groupboxFrameWaveform.Enabled =
//                                                                              radiobuttonRadioSine.Enabled =
//                                                                              radiobuttonTriangle.Enabled = true;

//                trackbarSlider1.Enabled = trackbarSlider2.Enabled = trackbarSlider3.Enabled =
//                                                                    trackbarSlider4.Enabled =
//                                                                    trackbarSlider5.Enabled = true;
//            }
//            labelParamName1.Text = "Wet/Dry Mix (%)";
//            labelParamValue1.Text = temp.WetDryMix.ToString();

//            labelParamName2.Text = "Depth (%)";
//            labelParamValue2.Text = temp.Depth.ToString();

//            labelParamName3.Text = "Feedback (%)";
//            labelParamValue3.Text = temp.Feedback.ToString();

//            labelParamName4.Text = "Frequency (Hz)";
//            labelParamValue4.Text = temp.Frequency.ToString();

//            labelParamName5.Text = "Delay (ms)";
//            labelParamValue5.Text = temp.Delay.ToString();
//        }
//        else if (typeof(GargleEffect) == efftype)
//        {
//            EffectsGargle temp = ((GargleEffect)eff).AllParameters;

//            if (MoveControls)
//            {
//                trackbarSlider1.Minimum = GargleEffect.RateHzMin;
//                trackbarSlider1.Maximum = GargleEffect.RateHzMax;
//                trackbarSlider1.Value = temp.RateHz;

//                if (GargleEffect.WaveSquare == temp.WaveShape)
//                    radiobuttonSquare.Checked = true;
//                else
//                    radiobuttonTriangle.Checked = true;

//                groupboxFrameWaveform.Enabled = radiobuttonSquare.Enabled = radiobuttonTriangle.Enabled = true;

//                trackbarSlider1.Enabled = true;
//            }
//            labelParamName1.Text = "Rate (Hz)";
//            labelParamValue1.Text = temp.RateHz.ToString();
//        }
//        else if (typeof(ParamEqEffect) == efftype)
//        {
//            EffectsParamEq temp = ((ParamEqEffect)eff).AllParameters;

//            if (MoveControls)
//            {
//                trackbarSlider1.Minimum = (int)ParamEqEffect.CenterMin;
//                trackbarSlider1.Maximum = (int)ParamEqEffect.CenterMax;
//                trackbarSlider1.Value = (int)temp.Center;
//                trackbarSlider2.Minimum = (int)ParamEqEffect.BandwidthMin;
//                trackbarSlider2.Maximum = (int)ParamEqEffect.BandwidthMax;
//                trackbarSlider2.Value = (int)temp.Bandwidth;
//                trackbarSlider3.Minimum = (int)ParamEqEffect.GainMin;
//                trackbarSlider3.Maximum = (int)ParamEqEffect.GainMax;
//                trackbarSlider3.Value = (int)temp.Gain;

//                trackbarSlider1.Enabled = trackbarSlider2.Enabled = trackbarSlider3.Enabled = true;
//            }
//            labelParamName1.Text = "Center Freq (Hz)";
//            labelParamValue1.Text = temp.Center.ToString();

//            labelParamName2.Text = "Bandwidth (Hz)";
//            labelParamValue2.Text = temp.Bandwidth.ToString();

//            labelParamName3.Text = "Gain (dB)";
//            labelParamValue3.Text = temp.Gain.ToString();
//        }
//        else if (typeof(WavesReverbEffect) == efftype)
//        {
//            EffectsWavesReverb temp = ((WavesReverbEffect)eff).AllParameters;

//            if (MoveControls)
//            {
//                trackbarSlider1.Minimum = (int)WavesReverbEffect.InGainMin;
//                trackbarSlider1.Maximum = (int)WavesReverbEffect.InGainMax;
//                trackbarSlider1.Value = (int)temp.InGain;
//                trackbarSlider2.Minimum = (int)WavesReverbEffect.ReverbMixMin;
//                trackbarSlider2.Maximum = (int)WavesReverbEffect.ReverbMixMax;
//                trackbarSlider2.Value = (int)temp.ReverbMix;
//                trackbarSlider3.Minimum = (int)(1000 * WavesReverbEffect.ReverbTimeMin);
//                trackbarSlider3.Maximum = (int)(1000 * WavesReverbEffect.ReverbTimeMax);
//                trackbarSlider3.Value = (int)(1000 * temp.ReverbTime);
//                trackbarSlider4.Minimum = (int)(1000 * WavesReverbEffect.HighFrequencyRtRatioMin);
//                trackbarSlider4.Maximum = (int)(1000 * WavesReverbEffect.HighFrequencyRtRatioMax);
//                trackbarSlider4.Value = (int)(1000 * temp.HighFrequencyRtRatio);

//                trackbarSlider1.Enabled =
//                    trackbarSlider2.Enabled = trackbarSlider3.Enabled = trackbarSlider4.Enabled = true;
//            }
//            labelParamName1.Text = "In Gain (dB)";
//            labelParamValue1.Text = temp.InGain.ToString();

//            labelParamName2.Text = "Waves Reverb Mix (dB)";
//            labelParamValue2.Text = temp.ReverbMix.ToString();

//            labelParamName3.Text = "Waves Reverb Time (ms)";
//            labelParamValue3.Text = temp.ReverbTime.ToString();

//            labelParamName4.Text = "HighFreq RT Ratio (x:1)";
//            labelParamValue4.Text = temp.HighFrequencyRtRatio.ToString();
//        }
//    }

//    private void ClearUI(bool ClearControls)
//    {
//        labelParamName1.Text = labelParamValue1.Text = labelParamName2.Text = labelParamValue2.Text =
//                                                                              labelParamName3.Text =
//                                                                              labelParamValue3.Text =
//                                                                              labelParamName4.Text =
//                                                                              labelParamValue4.Text =
//                                                                              labelParamName5.Text =
//                                                                              labelParamValue5.Text =
//                                                                              labelParamName6.Text =
//                                                                              labelParamName1.Text =
//                                                                              labelParamValue1.Text =
//                                                                              labelParamName2.Text =
//                                                                              labelParamValue2.Text =
//                                                                              labelParamName3.Text =
//                                                                              labelParamValue3.Text =
//                                                                              labelParamName4.Text =
//                                                                              labelParamValue4.Text =
//                                                                              labelParamName5.Text =
//                                                                              labelParamValue5.Text =
//                                                                              labelParamName6.Text =
//                                                                              labelParamValue6.Text =
//                                                                              labelParamName1.Text =
//                                                                              labelParamValue1.Text =
//                                                                              labelParamName2.Text =
//                                                                              labelParamValue2.Text =
//                                                                              labelParamName3.Text =
//                                                                              labelParamValue3.Text =
//                                                                              labelParamName4.Text =
//                                                                              labelParamValue4.Text =
//                                                                              labelParamName5.Text =
//                                                                              labelParamValue5.Text =
//                                                                              labelParamName6.Text =
//                                                                              labelParamName1.Text =
//                                                                              labelParamValue1.Text =
//                                                                              labelParamName2.Text =
//                                                                              labelParamValue2.Text =
//                                                                              labelParamName3.Text =
//                                                                              labelParamValue3.Text =
//                                                                              labelParamName4.Text =
//                                                                              labelParamValue4.Text =
//                                                                              labelParamName5.Text =
//                                                                              labelParamValue5.Text =
//                                                                              labelParamName6.Text =
//                                                                              labelParamName1.Text =
//                                                                              labelParamValue1.Text =
//                                                                              labelParamName2.Text =
//                                                                              labelParamValue2.Text =
//                                                                              labelParamName3.Text =
//                                                                              labelParamValue3.Text =
//                                                                              labelParamName4.Text =
//                                                                              labelParamValue4.Text =
//                                                                              labelParamName5.Text =
//                                                                              labelParamValue5.Text =
//                                                                              labelParamName6.Text =
//                                                                              labelParamName1.Text =
//                                                                              labelParamValue1.Text =
//                                                                              labelParamName2.Text =
//                                                                              labelParamValue2.Text =
//                                                                              labelParamName3.Text =
//                                                                              labelParamName4.Text =
//                                                                              labelParamValue4.Text =
//                                                                              labelParamName5.Text =
//                                                                              labelParamValue5.Text =
//                                                                              labelParamName6.Text =
//                                                                              labelParamValue6.Text =
//                                                                              labelParamName1.Text =
//                                                                              labelParamValue1.Text =
//                                                                              labelParamName2.Text =
//                                                                              labelParamValue2.Text =
//                                                                              labelParamName3.Text =
//                                                                              labelParamValue3.Text =
//                                                                              labelParamName4.Text =
//                                                                              labelParamValue4.Text =
//                                                                              labelParamName5.Text =
//                                                                              labelParamValue5.Text =
//                                                                              labelParamName6.Text =
//                                                                              labelParamValue6.Text =
//                                                                              labelParamName1.Text =
//                                                                              labelParamValue1.Text =
//                                                                              labelParamName2.Text =
//                                                                              labelParamValue2.Text =
//                                                                              labelParamName3.Text =
//                                                                              labelParamValue3.Text =
//                                                                              labelParamName4.Text =
//                                                                              labelParamValue4.Text =
//                                                                              labelParamName5.Text =
//                                                                              labelParamValue5.Text =
//                                                                              labelParamName6.Text =
//                                                                              labelParamValue6.Text = string.Empty;

//        if (ClearControls)
//        {
//            groupboxFrameWaveform.Enabled =
//                radiobuttonTriangle.Enabled = radiobuttonTriangle.Enabled = radiobuttonRadioSine.Enabled =
//                                                                            groupboxFramePhase.Enabled =
//                                                                            radiobuttonRadioNeg180.Enabled =
//                                                                            radiobuttonRadioNeg90.Enabled =
//                                                                            radiobuttonRadioZero.Enabled =
//                                                                            radiobuttonRadio90.Enabled =
//                                                                            radiobuttonRadio180.Enabled = false;

//            trackbarSlider1.Minimum = trackbarSlider2.Minimum = trackbarSlider3.Minimum =
//                                                                trackbarSlider4.Minimum =
//                                                                trackbarSlider5.Minimum = trackbarSlider6.Minimum = 0;
//            trackbarSlider1.Value = trackbarSlider2.Value = trackbarSlider3.Value =
//                                                            trackbarSlider4.Value =
//                                                            trackbarSlider5.Value = trackbarSlider6.Value = 0;
//            trackbarSlider1.Enabled = trackbarSlider2.Enabled = trackbarSlider3.Enabled =
//                                                                trackbarSlider4.Enabled =
//                                                                trackbarSlider5.Enabled =
//                                                                trackbarSlider6.Enabled = false;
//        }
//    }

//    private void trackbarSliderScroll(object sender, EventArgs e)
//    {
//        // We're ignoring settings right now
//        if (isIgnoringSettings)
//            return;

//        EffectInfo eff = (EffectInfo)effectDescription[currentIndex];
//        Type efftype = eff.Effect.GetType();

//        if (typeof(ChorusEffect) == efftype)
//        {
//            EffectsChorus temp = new EffectsChorus();
//            temp.WetDryMix = trackbarSlider1.Value;
//            temp.Frequency = trackbarSlider4.Value;
//            temp.Feedback = trackbarSlider3.Value;
//            temp.Depth = trackbarSlider2.Value;
//            temp.Delay = trackbarSlider5.Value;

//            if (radiobuttonRadioSine.Checked)
//                temp.Waveform = ChorusEffect.WaveSin;
//            else
//                temp.Waveform = ChorusEffect.WaveTriangle;

//            if (radiobuttonRadioNeg180.Checked)
//                temp.Phase = ChorusEffect.PhaseNegative180;
//            else if (radiobuttonRadioNeg90.Checked)
//                temp.Phase = ChorusEffect.PhaseNegative90;
//            else if (radiobuttonRadioZero.Checked)
//                temp.Phase = ChorusEffect.PhaseZero;
//            else if (radiobuttonRadio90.Checked)
//                temp.Phase = ChorusEffect.Phase90;
//            else if (radiobuttonRadio180.Checked)
//                temp.Phase = ChorusEffect.Phase180;

//            eff.EffectSettings = temp;
//            ((ChorusEffect)eff.Effect).AllParameters = temp;
//        }
//        else if (typeof(CompressorEffect) == efftype)
//        {
//            EffectsCompressor temp = new EffectsCompressor();
//            temp.Gain = trackbarSlider1.Value;
//            temp.Attack = trackbarSlider2.Value;
//            temp.Release = trackbarSlider3.Value;
//            temp.Threshold = trackbarSlider4.Value;
//            temp.Ratio = trackbarSlider5.Value;
//            temp.Predelay = trackbarSlider6.Value;

//            eff.EffectSettings = temp;
//            ((CompressorEffect)eff.Effect).AllParameters = temp;
//        }
//        else if (typeof(DistortionEffect) == efftype)
//        {
//            EffectsDistortion temp = new EffectsDistortion();
//            temp.Gain = trackbarSlider1.Value;
//            temp.Edge = trackbarSlider2.Value;
//            temp.PostEqCenterFrequency = trackbarSlider3.Value;
//            temp.PostEqBandwidth = trackbarSlider4.Value;
//            temp.PreLowpassCutoff = trackbarSlider5.Value;

//            eff.EffectSettings = temp;
//            ((DistortionEffect)eff.Effect).AllParameters = temp;
//        }
//        else if (typeof(EchoEffect) == efftype)
//        {
//            EffectsEcho temp = new EffectsEcho();
//            temp.WetDryMix = trackbarSlider1.Value;
//            temp.Feedback = trackbarSlider2.Value;
//            temp.LeftDelay = trackbarSlider3.Value;
//            temp.RightDelay = trackbarSlider4.Value;
//            temp.PanDelay = trackbarSlider5.Value;

//            eff.EffectSettings = temp;
//            ((EchoEffect)eff.Effect).AllParameters = temp;
//        }
//        else if (typeof(FlangerEffect) == efftype)
//        {
//            EffectsFlanger temp = new EffectsFlanger();
//            temp.WetDryMix = trackbarSlider1.Value;
//            temp.Depth = trackbarSlider2.Value;
//            temp.Feedback = trackbarSlider3.Value;
//            temp.Frequency = trackbarSlider4.Value;
//            temp.Delay = trackbarSlider5.Value;

//            if (radiobuttonRadioSine.Checked)
//                temp.Waveform = FlangerEffect.WaveSin;
//            else
//                temp.Waveform = FlangerEffect.WaveTriangle;

//            if (radiobuttonRadioNeg180.Checked)
//                temp.Phase = ChorusEffect.PhaseNegative180;
//            else if (radiobuttonRadioNeg90.Checked)
//                temp.Phase = ChorusEffect.PhaseNegative90;
//            else if (radiobuttonRadioZero.Checked)
//                temp.Phase = ChorusEffect.PhaseZero;
//            else if (radiobuttonRadio90.Checked)
//                temp.Phase = ChorusEffect.Phase90;
//            else if (radiobuttonRadio180.Checked)
//                temp.Phase = ChorusEffect.Phase180;

//            eff.EffectSettings = temp;
//            ((FlangerEffect)eff.Effect).AllParameters = temp;
//        }
//        else if (typeof(GargleEffect) == efftype)
//        {
//            EffectsGargle temp = new EffectsGargle();
//            temp.RateHz = trackbarSlider1.Value;
//            if (radiobuttonSquare.Checked)
//                temp.WaveShape = GargleEffect.WaveSquare;
//            else
//                temp.WaveShape = GargleEffect.WaveTriangle;

//            if (radiobuttonSquare.Checked)
//                temp.WaveShape = GargleEffect.WaveSquare;
//            else
//                temp.WaveShape = GargleEffect.WaveTriangle;

//            eff.EffectSettings = temp;
//            ((GargleEffect)eff.Effect).AllParameters = temp;
//        }
//        else if (typeof(ParamEqEffect) == efftype)
//        {
//            EffectsParamEq temp = new EffectsParamEq();
//            temp.Center = trackbarSlider1.Value;
//            temp.Bandwidth = trackbarSlider2.Value;
//            temp.Gain = trackbarSlider3.Value;

//            eff.EffectSettings = temp;
//            ((ParamEqEffect)eff.Effect).AllParameters = temp;
//        }
//        else if (typeof(WavesReverbEffect) == efftype)
//        {
//            EffectsWavesReverb temp = new EffectsWavesReverb();
//            temp.InGain = trackbarSlider1.Value;
//            temp.ReverbMix = trackbarSlider2.Value;
//            temp.ReverbTime = (double)(.001 * trackbarSlider3.Value);
//            temp.HighFrequencyRtRatio = (double)(.001 * trackbarSlider4.Value);

//            eff.EffectSettings = temp;
//            ((WavesReverbEffect)eff.Effect).AllParameters = temp;
//        }
//        effectDescription[currentIndex] = eff;
//        UpdateUI(false);
//    }

//    private void DeleteEffect()
//    {
//        EffectInfo[] temp;

//        if (-1 == listboxEffects.SelectedIndex)
//            return;

//        effectDescription.RemoveAt(listboxEffects.SelectedIndex);

//        if (effectDescription.Count > 0)
//        {
//            temp = new EffectInfo[effectDescription.Count];
//            effectDescription.CopyTo(temp, 0);
//            AddEffect(temp);
//            listboxEffects.Items.RemoveAt(listboxEffects.SelectedIndex);
//            listboxEffects.SelectedIndex = currentIndex = 0;
//        }
//        else
//        {
//            temp = null;
//            AddEffect(temp);
//            listboxEffects.Items.Clear();
//            ClearUI(true);
//        }
//        effectDescription.Clear();
//        if (null != temp)
//            effectDescription.AddRange(temp);
//    }

//    private void listboxEffects_KeyUp(object sender, KeyEventArgs e)
//    {
//        if (e.KeyCode == Keys.Delete)
//            DeleteEffect();
//    }

//    private void buttonOk_Click(object sender, EventArgs e)
//    {
//        Close();
//    }

//    private void buttonDelete_Click(object sender, EventArgs e)
//    {
//        DeleteEffect();
//    }

//    private void timer1_Tick(object sender, EventArgs e)
//    {
//        if (applicationBuffer.Status.Playing)
//            return;
//        else
//        {
//            timer1.Enabled = false;
//            buttonStop.Enabled = false;
//            buttonPlay.Enabled = true;
//            comboEffects.Enabled = true;

//            labelTextStatus.Text = "Sound stopped.";
//        }
//    }

//    private void MainForm_Closing(object sender, CancelEventArgs e)
//    {
//        if (null != applicationBuffer)
//        {
//            if (applicationBuffer.Status.Playing)
//                applicationBuffer.Stop();
//        }
//    }
//}
