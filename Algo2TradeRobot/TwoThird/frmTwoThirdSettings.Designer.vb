<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmTwoThirdSettings
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmTwoThirdSettings))
        Me.ImageList1 = New System.Windows.Forms.ImageList(Me.components)
        Me.opnFileSettings = New System.Windows.Forms.OpenFileDialog()
        Me.txtATRPeriod = New System.Windows.Forms.TextBox()
        Me.lblATRPeriod = New System.Windows.Forms.Label()
        Me.btnTwoThirdStrayegySettings = New System.Windows.Forms.Button()
        Me.txtTargetMultiplier = New System.Windows.Forms.TextBox()
        Me.dtpckrEODExitTime = New System.Windows.Forms.DateTimePicker()
        Me.dtpckrLastTradeEntryTime = New System.Windows.Forms.DateTimePicker()
        Me.lblEODExitTime = New System.Windows.Forms.Label()
        Me.btnBrowse = New System.Windows.Forms.Button()
        Me.txtInstrumentDetalis = New System.Windows.Forms.TextBox()
        Me.lblInstrumentDetails = New System.Windows.Forms.Label()
        Me.txtSignalTimeFrame = New System.Windows.Forms.TextBox()
        Me.lblSignalTimeFrame = New System.Windows.Forms.Label()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.txtMaxProfitPerDay = New System.Windows.Forms.TextBox()
        Me.lblMaxProfitPerDay = New System.Windows.Forms.Label()
        Me.txtMaxLossPerDay = New System.Windows.Forms.TextBox()
        Me.lblMaxLossPerDay = New System.Windows.Forms.Label()
        Me.lblTargetMultiplier = New System.Windows.Forms.Label()
        Me.txtNumberOfTradePerStock = New System.Windows.Forms.TextBox()
        Me.lblNumberOfTradePerStock = New System.Windows.Forms.Label()
        Me.dtpckrTradeStartTime = New System.Windows.Forms.DateTimePicker()
        Me.lblLastTradeEntryTime = New System.Windows.Forms.Label()
        Me.lblTradeStartTime = New System.Windows.Forms.Label()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.chbCountTradesWithBreakevenMovement = New System.Windows.Forms.CheckBox()
        Me.chbStoplossMoveToBreakeven = New System.Windows.Forms.CheckBox()
        Me.chbReverseTrade = New System.Windows.Forms.CheckBox()
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
        Me.txtNumberOfStock = New System.Windows.Forms.TextBox()
        Me.lblNumberOfStock = New System.Windows.Forms.Label()
        Me.txtMinVolume = New System.Windows.Forms.TextBox()
        Me.lblMinVolume = New System.Windows.Forms.Label()
        Me.txtATRPercentage = New System.Windows.Forms.TextBox()
        Me.lblATR = New System.Windows.Forms.Label()
        Me.txtMaxPrice = New System.Windows.Forms.TextBox()
        Me.lblMaxPrice = New System.Windows.Forms.Label()
        Me.txtMinPrice = New System.Windows.Forms.TextBox()
        Me.lblMinPrice = New System.Windows.Forms.Label()
        Me.GroupBox4 = New System.Windows.Forms.GroupBox()
        Me.chbAutoSelectStock = New System.Windows.Forms.CheckBox()
        Me.txtManualStockList = New System.Windows.Forms.TextBox()
        Me.lblManualStock = New System.Windows.Forms.Label()
        Me.txtFutureMinCapital = New System.Windows.Forms.TextBox()
        Me.lblFutureMinCapital = New System.Windows.Forms.Label()
        Me.chbFuture = New System.Windows.Forms.CheckBox()
        Me.chbCash = New System.Windows.Forms.CheckBox()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.GroupBox3.SuspendLayout()
        Me.GroupBox4.SuspendLayout()
        Me.SuspendLayout()
        '
        'ImageList1
        '
        Me.ImageList1.ImageStream = CType(resources.GetObject("ImageList1.ImageStream"), System.Windows.Forms.ImageListStreamer)
        Me.ImageList1.TransparentColor = System.Drawing.Color.Transparent
        Me.ImageList1.Images.SetKeyName(0, "save-icon-36533.png")
        '
        'opnFileSettings
        '
        '
        'txtATRPeriod
        '
        Me.txtATRPeriod.Location = New System.Drawing.Point(199, 15)
        Me.txtATRPeriod.Margin = New System.Windows.Forms.Padding(4)
        Me.txtATRPeriod.Name = "txtATRPeriod"
        Me.txtATRPeriod.Size = New System.Drawing.Size(255, 22)
        Me.txtATRPeriod.TabIndex = 0
        Me.txtATRPeriod.Tag = "Signal Time Frame"
        '
        'lblATRPeriod
        '
        Me.lblATRPeriod.AutoSize = True
        Me.lblATRPeriod.Location = New System.Drawing.Point(9, 18)
        Me.lblATRPeriod.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblATRPeriod.Name = "lblATRPeriod"
        Me.lblATRPeriod.Size = New System.Drawing.Size(81, 17)
        Me.lblATRPeriod.TabIndex = 33
        Me.lblATRPeriod.Text = "ATR Period"
        '
        'btnTwoThirdStrayegySettings
        '
        Me.btnTwoThirdStrayegySettings.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnTwoThirdStrayegySettings.ImageKey = "save-icon-36533.png"
        Me.btnTwoThirdStrayegySettings.ImageList = Me.ImageList1
        Me.btnTwoThirdStrayegySettings.Location = New System.Drawing.Point(859, 13)
        Me.btnTwoThirdStrayegySettings.Margin = New System.Windows.Forms.Padding(4)
        Me.btnTwoThirdStrayegySettings.Name = "btnTwoThirdStrayegySettings"
        Me.btnTwoThirdStrayegySettings.Size = New System.Drawing.Size(112, 58)
        Me.btnTwoThirdStrayegySettings.TabIndex = 27
        Me.btnTwoThirdStrayegySettings.Text = "&Save"
        Me.btnTwoThirdStrayegySettings.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnTwoThirdStrayegySettings.UseVisualStyleBackColor = True
        '
        'txtTargetMultiplier
        '
        Me.txtTargetMultiplier.Location = New System.Drawing.Point(199, 230)
        Me.txtTargetMultiplier.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTargetMultiplier.Name = "txtTargetMultiplier"
        Me.txtTargetMultiplier.Size = New System.Drawing.Size(255, 22)
        Me.txtTargetMultiplier.TabIndex = 6
        Me.txtTargetMultiplier.Tag = "Max Profit Per Day"
        '
        'dtpckrEODExitTime
        '
        Me.dtpckrEODExitTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrEODExitTime.Location = New System.Drawing.Point(199, 160)
        Me.dtpckrEODExitTime.Name = "dtpckrEODExitTime"
        Me.dtpckrEODExitTime.ShowUpDown = True
        Me.dtpckrEODExitTime.Size = New System.Drawing.Size(255, 22)
        Me.dtpckrEODExitTime.TabIndex = 4
        Me.dtpckrEODExitTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'dtpckrLastTradeEntryTime
        '
        Me.dtpckrLastTradeEntryTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrLastTradeEntryTime.Location = New System.Drawing.Point(200, 125)
        Me.dtpckrLastTradeEntryTime.Name = "dtpckrLastTradeEntryTime"
        Me.dtpckrLastTradeEntryTime.ShowUpDown = True
        Me.dtpckrLastTradeEntryTime.Size = New System.Drawing.Size(255, 22)
        Me.dtpckrLastTradeEntryTime.TabIndex = 3
        Me.dtpckrLastTradeEntryTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'lblEODExitTime
        '
        Me.lblEODExitTime.AutoSize = True
        Me.lblEODExitTime.Location = New System.Drawing.Point(9, 161)
        Me.lblEODExitTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblEODExitTime.Name = "lblEODExitTime"
        Me.lblEODExitTime.Size = New System.Drawing.Size(99, 17)
        Me.lblEODExitTime.TabIndex = 23
        Me.lblEODExitTime.Text = "EOD Exit Time"
        '
        'btnBrowse
        '
        Me.btnBrowse.Font = New System.Drawing.Font("Microsoft Sans Serif", 7.8!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnBrowse.Location = New System.Drawing.Point(428, 331)
        Me.btnBrowse.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.btnBrowse.Name = "btnBrowse"
        Me.btnBrowse.Size = New System.Drawing.Size(40, 23)
        Me.btnBrowse.TabIndex = 8
        Me.btnBrowse.Text = "..."
        Me.btnBrowse.UseVisualStyleBackColor = True
        '
        'txtInstrumentDetalis
        '
        Me.txtInstrumentDetalis.Location = New System.Drawing.Point(198, 332)
        Me.txtInstrumentDetalis.Margin = New System.Windows.Forms.Padding(4)
        Me.txtInstrumentDetalis.Name = "txtInstrumentDetalis"
        Me.txtInstrumentDetalis.ReadOnly = True
        Me.txtInstrumentDetalis.Size = New System.Drawing.Size(223, 22)
        Me.txtInstrumentDetalis.TabIndex = 15
        '
        'lblInstrumentDetails
        '
        Me.lblInstrumentDetails.AutoSize = True
        Me.lblInstrumentDetails.Location = New System.Drawing.Point(8, 335)
        Me.lblInstrumentDetails.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblInstrumentDetails.Name = "lblInstrumentDetails"
        Me.lblInstrumentDetails.Size = New System.Drawing.Size(121, 17)
        Me.lblInstrumentDetails.TabIndex = 8
        Me.lblInstrumentDetails.Text = "Instrument Details"
        '
        'txtSignalTimeFrame
        '
        Me.txtSignalTimeFrame.Location = New System.Drawing.Point(199, 51)
        Me.txtSignalTimeFrame.Margin = New System.Windows.Forms.Padding(4)
        Me.txtSignalTimeFrame.Name = "txtSignalTimeFrame"
        Me.txtSignalTimeFrame.Size = New System.Drawing.Size(255, 22)
        Me.txtSignalTimeFrame.TabIndex = 1
        Me.txtSignalTimeFrame.Tag = "Signal Time Frame"
        '
        'lblSignalTimeFrame
        '
        Me.lblSignalTimeFrame.AutoSize = True
        Me.lblSignalTimeFrame.Location = New System.Drawing.Point(9, 54)
        Me.lblSignalTimeFrame.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblSignalTimeFrame.Name = "lblSignalTimeFrame"
        Me.lblSignalTimeFrame.Size = New System.Drawing.Size(158, 17)
        Me.lblSignalTimeFrame.TabIndex = 3
        Me.lblSignalTimeFrame.Text = "Signal Time Frame(min)"
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.txtMaxProfitPerDay)
        Me.GroupBox1.Controls.Add(Me.lblMaxProfitPerDay)
        Me.GroupBox1.Controls.Add(Me.txtMaxLossPerDay)
        Me.GroupBox1.Controls.Add(Me.lblMaxLossPerDay)
        Me.GroupBox1.Controls.Add(Me.txtATRPeriod)
        Me.GroupBox1.Controls.Add(Me.lblATRPeriod)
        Me.GroupBox1.Controls.Add(Me.txtTargetMultiplier)
        Me.GroupBox1.Controls.Add(Me.lblTargetMultiplier)
        Me.GroupBox1.Controls.Add(Me.txtNumberOfTradePerStock)
        Me.GroupBox1.Controls.Add(Me.lblNumberOfTradePerStock)
        Me.GroupBox1.Controls.Add(Me.dtpckrEODExitTime)
        Me.GroupBox1.Controls.Add(Me.dtpckrLastTradeEntryTime)
        Me.GroupBox1.Controls.Add(Me.dtpckrTradeStartTime)
        Me.GroupBox1.Controls.Add(Me.lblEODExitTime)
        Me.GroupBox1.Controls.Add(Me.lblLastTradeEntryTime)
        Me.GroupBox1.Controls.Add(Me.lblTradeStartTime)
        Me.GroupBox1.Controls.Add(Me.btnBrowse)
        Me.GroupBox1.Controls.Add(Me.txtInstrumentDetalis)
        Me.GroupBox1.Controls.Add(Me.lblInstrumentDetails)
        Me.GroupBox1.Controls.Add(Me.txtSignalTimeFrame)
        Me.GroupBox1.Controls.Add(Me.lblSignalTimeFrame)
        Me.GroupBox1.Location = New System.Drawing.Point(8, 4)
        Me.GroupBox1.Margin = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Padding = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Size = New System.Drawing.Size(477, 367)
        Me.GroupBox1.TabIndex = 28
        Me.GroupBox1.TabStop = False
        '
        'txtMaxProfitPerDay
        '
        Me.txtMaxProfitPerDay.Location = New System.Drawing.Point(199, 298)
        Me.txtMaxProfitPerDay.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxProfitPerDay.Name = "txtMaxProfitPerDay"
        Me.txtMaxProfitPerDay.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxProfitPerDay.TabIndex = 35
        '
        'lblMaxProfitPerDay
        '
        Me.lblMaxProfitPerDay.AutoSize = True
        Me.lblMaxProfitPerDay.Location = New System.Drawing.Point(10, 301)
        Me.lblMaxProfitPerDay.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxProfitPerDay.Name = "lblMaxProfitPerDay"
        Me.lblMaxProfitPerDay.Size = New System.Drawing.Size(125, 17)
        Me.lblMaxProfitPerDay.TabIndex = 37
        Me.lblMaxProfitPerDay.Text = "Max Profit Per Day"
        '
        'txtMaxLossPerDay
        '
        Me.txtMaxLossPerDay.Location = New System.Drawing.Point(199, 263)
        Me.txtMaxLossPerDay.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxLossPerDay.Name = "txtMaxLossPerDay"
        Me.txtMaxLossPerDay.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxLossPerDay.TabIndex = 34
        '
        'lblMaxLossPerDay
        '
        Me.lblMaxLossPerDay.AutoSize = True
        Me.lblMaxLossPerDay.Location = New System.Drawing.Point(10, 267)
        Me.lblMaxLossPerDay.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxLossPerDay.Name = "lblMaxLossPerDay"
        Me.lblMaxLossPerDay.Size = New System.Drawing.Size(122, 17)
        Me.lblMaxLossPerDay.TabIndex = 36
        Me.lblMaxLossPerDay.Text = "Max Loss Per Day"
        '
        'lblTargetMultiplier
        '
        Me.lblTargetMultiplier.AutoSize = True
        Me.lblTargetMultiplier.Location = New System.Drawing.Point(9, 234)
        Me.lblTargetMultiplier.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTargetMultiplier.Name = "lblTargetMultiplier"
        Me.lblTargetMultiplier.Size = New System.Drawing.Size(110, 17)
        Me.lblTargetMultiplier.TabIndex = 31
        Me.lblTargetMultiplier.Text = "Target Multiplier"
        '
        'txtNumberOfTradePerStock
        '
        Me.txtNumberOfTradePerStock.Location = New System.Drawing.Point(199, 195)
        Me.txtNumberOfTradePerStock.Margin = New System.Windows.Forms.Padding(4)
        Me.txtNumberOfTradePerStock.Name = "txtNumberOfTradePerStock"
        Me.txtNumberOfTradePerStock.Size = New System.Drawing.Size(255, 22)
        Me.txtNumberOfTradePerStock.TabIndex = 5
        Me.txtNumberOfTradePerStock.Tag = "Max Loss Per Day"
        '
        'lblNumberOfTradePerStock
        '
        Me.lblNumberOfTradePerStock.AutoSize = True
        Me.lblNumberOfTradePerStock.Location = New System.Drawing.Point(9, 199)
        Me.lblNumberOfTradePerStock.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblNumberOfTradePerStock.Name = "lblNumberOfTradePerStock"
        Me.lblNumberOfTradePerStock.Size = New System.Drawing.Size(184, 17)
        Me.lblNumberOfTradePerStock.TabIndex = 30
        Me.lblNumberOfTradePerStock.Text = "Number Of Trade Per Stock"
        '
        'dtpckrTradeStartTime
        '
        Me.dtpckrTradeStartTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrTradeStartTime.Location = New System.Drawing.Point(199, 88)
        Me.dtpckrTradeStartTime.Name = "dtpckrTradeStartTime"
        Me.dtpckrTradeStartTime.ShowUpDown = True
        Me.dtpckrTradeStartTime.Size = New System.Drawing.Size(255, 22)
        Me.dtpckrTradeStartTime.TabIndex = 2
        Me.dtpckrTradeStartTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'lblLastTradeEntryTime
        '
        Me.lblLastTradeEntryTime.AutoSize = True
        Me.lblLastTradeEntryTime.Location = New System.Drawing.Point(9, 126)
        Me.lblLastTradeEntryTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblLastTradeEntryTime.Name = "lblLastTradeEntryTime"
        Me.lblLastTradeEntryTime.Size = New System.Drawing.Size(149, 17)
        Me.lblLastTradeEntryTime.TabIndex = 21
        Me.lblLastTradeEntryTime.Text = "Last Trade Entry Time"
        '
        'lblTradeStartTime
        '
        Me.lblTradeStartTime.AutoSize = True
        Me.lblTradeStartTime.Location = New System.Drawing.Point(9, 90)
        Me.lblTradeStartTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTradeStartTime.Name = "lblTradeStartTime"
        Me.lblTradeStartTime.Size = New System.Drawing.Size(115, 17)
        Me.lblTradeStartTime.TabIndex = 19
        Me.lblTradeStartTime.Text = "Trade Start Time"
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.chbCountTradesWithBreakevenMovement)
        Me.GroupBox2.Controls.Add(Me.chbStoplossMoveToBreakeven)
        Me.GroupBox2.Controls.Add(Me.chbReverseTrade)
        Me.GroupBox2.Location = New System.Drawing.Point(493, 5)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(358, 125)
        Me.GroupBox2.TabIndex = 30
        Me.GroupBox2.TabStop = False
        '
        'chbCountTradesWithBreakevenMovement
        '
        Me.chbCountTradesWithBreakevenMovement.AutoSize = True
        Me.chbCountTradesWithBreakevenMovement.Location = New System.Drawing.Point(18, 84)
        Me.chbCountTradesWithBreakevenMovement.Name = "chbCountTradesWithBreakevenMovement"
        Me.chbCountTradesWithBreakevenMovement.Size = New System.Drawing.Size(289, 21)
        Me.chbCountTradesWithBreakevenMovement.TabIndex = 2
        Me.chbCountTradesWithBreakevenMovement.Text = "Count Trades With Breakeven Movement"
        Me.chbCountTradesWithBreakevenMovement.UseVisualStyleBackColor = True
        '
        'chbStoplossMoveToBreakeven
        '
        Me.chbStoplossMoveToBreakeven.AutoSize = True
        Me.chbStoplossMoveToBreakeven.Location = New System.Drawing.Point(18, 50)
        Me.chbStoplossMoveToBreakeven.Name = "chbStoplossMoveToBreakeven"
        Me.chbStoplossMoveToBreakeven.Size = New System.Drawing.Size(246, 21)
        Me.chbStoplossMoveToBreakeven.TabIndex = 1
        Me.chbStoplossMoveToBreakeven.Text = "Stoploss Movement To Breakeven"
        Me.chbStoplossMoveToBreakeven.UseVisualStyleBackColor = True
        '
        'chbReverseTrade
        '
        Me.chbReverseTrade.AutoSize = True
        Me.chbReverseTrade.Location = New System.Drawing.Point(18, 17)
        Me.chbReverseTrade.Name = "chbReverseTrade"
        Me.chbReverseTrade.Size = New System.Drawing.Size(125, 21)
        Me.chbReverseTrade.TabIndex = 0
        Me.chbReverseTrade.Text = "Reverse Trade"
        Me.chbReverseTrade.UseVisualStyleBackColor = True
        '
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.txtNumberOfStock)
        Me.GroupBox3.Controls.Add(Me.lblNumberOfStock)
        Me.GroupBox3.Controls.Add(Me.txtMinVolume)
        Me.GroupBox3.Controls.Add(Me.lblMinVolume)
        Me.GroupBox3.Controls.Add(Me.txtATRPercentage)
        Me.GroupBox3.Controls.Add(Me.lblATR)
        Me.GroupBox3.Controls.Add(Me.txtMaxPrice)
        Me.GroupBox3.Controls.Add(Me.lblMaxPrice)
        Me.GroupBox3.Controls.Add(Me.txtMinPrice)
        Me.GroupBox3.Controls.Add(Me.lblMinPrice)
        Me.GroupBox3.Location = New System.Drawing.Point(8, 378)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(843, 100)
        Me.GroupBox3.TabIndex = 36
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "Stock Selection Settings"
        '
        'txtNumberOfStock
        '
        Me.txtNumberOfStock.Location = New System.Drawing.Point(133, 63)
        Me.txtNumberOfStock.Margin = New System.Windows.Forms.Padding(4)
        Me.txtNumberOfStock.Name = "txtNumberOfStock"
        Me.txtNumberOfStock.Size = New System.Drawing.Size(122, 22)
        Me.txtNumberOfStock.TabIndex = 42
        Me.txtNumberOfStock.Tag = "Number Of Stock"
        '
        'lblNumberOfStock
        '
        Me.lblNumberOfStock.AutoSize = True
        Me.lblNumberOfStock.Location = New System.Drawing.Point(9, 66)
        Me.lblNumberOfStock.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblNumberOfStock.Name = "lblNumberOfStock"
        Me.lblNumberOfStock.Size = New System.Drawing.Size(116, 17)
        Me.lblNumberOfStock.TabIndex = 43
        Me.lblNumberOfStock.Text = "Number Of Stock"
        '
        'txtMinVolume
        '
        Me.txtMinVolume.Location = New System.Drawing.Point(704, 26)
        Me.txtMinVolume.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMinVolume.Name = "txtMinVolume"
        Me.txtMinVolume.Size = New System.Drawing.Size(122, 22)
        Me.txtMinVolume.TabIndex = 40
        Me.txtMinVolume.Tag = "Min Volume"
        '
        'lblMinVolume
        '
        Me.lblMinVolume.AutoSize = True
        Me.lblMinVolume.Location = New System.Drawing.Point(615, 29)
        Me.lblMinVolume.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMinVolume.Name = "lblMinVolume"
        Me.lblMinVolume.Size = New System.Drawing.Size(81, 17)
        Me.lblMinVolume.TabIndex = 41
        Me.lblMinVolume.Text = "Min Volume"
        '
        'txtATRPercentage
        '
        Me.txtATRPercentage.Location = New System.Drawing.Point(480, 26)
        Me.txtATRPercentage.Margin = New System.Windows.Forms.Padding(4)
        Me.txtATRPercentage.Name = "txtATRPercentage"
        Me.txtATRPercentage.Size = New System.Drawing.Size(114, 22)
        Me.txtATRPercentage.TabIndex = 38
        Me.txtATRPercentage.Tag = "ATR %"
        '
        'lblATR
        '
        Me.lblATR.AutoSize = True
        Me.lblATR.Location = New System.Drawing.Point(417, 29)
        Me.lblATR.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblATR.Name = "lblATR"
        Me.lblATR.Size = New System.Drawing.Size(52, 17)
        Me.lblATR.TabIndex = 39
        Me.lblATR.Text = "ATR %"
        '
        'txtMaxPrice
        '
        Me.txtMaxPrice.Location = New System.Drawing.Point(288, 25)
        Me.txtMaxPrice.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxPrice.Name = "txtMaxPrice"
        Me.txtMaxPrice.Size = New System.Drawing.Size(114, 22)
        Me.txtMaxPrice.TabIndex = 36
        Me.txtMaxPrice.Tag = "Max Price"
        '
        'lblMaxPrice
        '
        Me.lblMaxPrice.AutoSize = True
        Me.lblMaxPrice.Location = New System.Drawing.Point(211, 28)
        Me.lblMaxPrice.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxPrice.Name = "lblMaxPrice"
        Me.lblMaxPrice.Size = New System.Drawing.Size(69, 17)
        Me.lblMaxPrice.TabIndex = 37
        Me.lblMaxPrice.Text = "Max Price"
        '
        'txtMinPrice
        '
        Me.txtMinPrice.Location = New System.Drawing.Point(86, 25)
        Me.txtMinPrice.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMinPrice.Name = "txtMinPrice"
        Me.txtMinPrice.Size = New System.Drawing.Size(114, 22)
        Me.txtMinPrice.TabIndex = 34
        Me.txtMinPrice.Tag = "Min Price"
        '
        'lblMinPrice
        '
        Me.lblMinPrice.AutoSize = True
        Me.lblMinPrice.Location = New System.Drawing.Point(9, 28)
        Me.lblMinPrice.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMinPrice.Name = "lblMinPrice"
        Me.lblMinPrice.Size = New System.Drawing.Size(66, 17)
        Me.lblMinPrice.TabIndex = 35
        Me.lblMinPrice.Text = "Min Price"
        '
        'GroupBox4
        '
        Me.GroupBox4.Controls.Add(Me.chbAutoSelectStock)
        Me.GroupBox4.Controls.Add(Me.txtManualStockList)
        Me.GroupBox4.Controls.Add(Me.lblManualStock)
        Me.GroupBox4.Controls.Add(Me.txtFutureMinCapital)
        Me.GroupBox4.Controls.Add(Me.lblFutureMinCapital)
        Me.GroupBox4.Controls.Add(Me.chbFuture)
        Me.GroupBox4.Controls.Add(Me.chbCash)
        Me.GroupBox4.Location = New System.Drawing.Point(493, 130)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(358, 242)
        Me.GroupBox4.TabIndex = 37
        Me.GroupBox4.TabStop = False
        '
        'chbAutoSelectStock
        '
        Me.chbAutoSelectStock.AutoSize = True
        Me.chbAutoSelectStock.Location = New System.Drawing.Point(12, 14)
        Me.chbAutoSelectStock.Name = "chbAutoSelectStock"
        Me.chbAutoSelectStock.Size = New System.Drawing.Size(141, 21)
        Me.chbAutoSelectStock.TabIndex = 40
        Me.chbAutoSelectStock.Text = "Auto Select Stock"
        Me.chbAutoSelectStock.UseVisualStyleBackColor = True
        '
        'txtManualStockList
        '
        Me.txtManualStockList.Location = New System.Drawing.Point(146, 111)
        Me.txtManualStockList.Margin = New System.Windows.Forms.Padding(4)
        Me.txtManualStockList.Multiline = True
        Me.txtManualStockList.Name = "txtManualStockList"
        Me.txtManualStockList.Size = New System.Drawing.Size(201, 117)
        Me.txtManualStockList.TabIndex = 38
        Me.txtManualStockList.Tag = "Signal Time Frame"
        '
        'lblManualStock
        '
        Me.lblManualStock.AutoSize = True
        Me.lblManualStock.Location = New System.Drawing.Point(6, 114)
        Me.lblManualStock.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblManualStock.Name = "lblManualStock"
        Me.lblManualStock.Size = New System.Drawing.Size(119, 17)
        Me.lblManualStock.TabIndex = 39
        Me.lblManualStock.Text = "Manual Stock List"
        '
        'txtFutureMinCapital
        '
        Me.txtFutureMinCapital.Location = New System.Drawing.Point(146, 79)
        Me.txtFutureMinCapital.Margin = New System.Windows.Forms.Padding(4)
        Me.txtFutureMinCapital.Name = "txtFutureMinCapital"
        Me.txtFutureMinCapital.Size = New System.Drawing.Size(201, 22)
        Me.txtFutureMinCapital.TabIndex = 36
        Me.txtFutureMinCapital.Tag = "Signal Time Frame"
        '
        'lblFutureMinCapital
        '
        Me.lblFutureMinCapital.AutoSize = True
        Me.lblFutureMinCapital.Location = New System.Drawing.Point(6, 82)
        Me.lblFutureMinCapital.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblFutureMinCapital.Name = "lblFutureMinCapital"
        Me.lblFutureMinCapital.Size = New System.Drawing.Size(77, 17)
        Me.lblFutureMinCapital.TabIndex = 37
        Me.lblFutureMinCapital.Text = "Min Capital"
        '
        'chbFuture
        '
        Me.chbFuture.AutoSize = True
        Me.chbFuture.Location = New System.Drawing.Point(84, 43)
        Me.chbFuture.Name = "chbFuture"
        Me.chbFuture.Size = New System.Drawing.Size(71, 21)
        Me.chbFuture.TabIndex = 1
        Me.chbFuture.Text = "Future"
        Me.chbFuture.UseVisualStyleBackColor = True
        '
        'chbCash
        '
        Me.chbCash.AutoSize = True
        Me.chbCash.Location = New System.Drawing.Point(12, 42)
        Me.chbCash.Name = "chbCash"
        Me.chbCash.Size = New System.Drawing.Size(62, 21)
        Me.chbCash.TabIndex = 0
        Me.chbCash.Text = "Cash"
        Me.chbCash.UseVisualStyleBackColor = True
        '
        'frmTwoThirdSettings
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(975, 484)
        Me.Controls.Add(Me.GroupBox4)
        Me.Controls.Add(Me.GroupBox3)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.btnTwoThirdStrayegySettings)
        Me.Controls.Add(Me.GroupBox1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "frmTwoThirdSettings"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "TwoThird Strategy - Settings"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.GroupBox3.ResumeLayout(False)
        Me.GroupBox3.PerformLayout()
        Me.GroupBox4.ResumeLayout(False)
        Me.GroupBox4.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents ImageList1 As ImageList
    Friend WithEvents opnFileSettings As OpenFileDialog
    Friend WithEvents txtATRPeriod As TextBox
    Friend WithEvents lblATRPeriod As Label
    Friend WithEvents btnTwoThirdStrayegySettings As Button
    Friend WithEvents txtTargetMultiplier As TextBox
    Friend WithEvents dtpckrEODExitTime As DateTimePicker
    Friend WithEvents dtpckrLastTradeEntryTime As DateTimePicker
    Friend WithEvents lblEODExitTime As Label
    Friend WithEvents btnBrowse As Button
    Friend WithEvents txtInstrumentDetalis As TextBox
    Friend WithEvents lblInstrumentDetails As Label
    Friend WithEvents txtSignalTimeFrame As TextBox
    Friend WithEvents lblSignalTimeFrame As Label
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents lblTargetMultiplier As Label
    Friend WithEvents txtNumberOfTradePerStock As TextBox
    Friend WithEvents lblNumberOfTradePerStock As Label
    Friend WithEvents dtpckrTradeStartTime As DateTimePicker
    Friend WithEvents lblLastTradeEntryTime As Label
    Friend WithEvents lblTradeStartTime As Label
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents chbStoplossMoveToBreakeven As CheckBox
    Friend WithEvents chbReverseTrade As CheckBox
    Friend WithEvents chbCountTradesWithBreakevenMovement As CheckBox
    Friend WithEvents txtMaxProfitPerDay As TextBox
    Friend WithEvents lblMaxProfitPerDay As Label
    Friend WithEvents txtMaxLossPerDay As TextBox
    Friend WithEvents lblMaxLossPerDay As Label
    Friend WithEvents GroupBox3 As GroupBox
    Friend WithEvents txtNumberOfStock As TextBox
    Friend WithEvents lblNumberOfStock As Label
    Friend WithEvents txtMinVolume As TextBox
    Friend WithEvents lblMinVolume As Label
    Friend WithEvents txtATRPercentage As TextBox
    Friend WithEvents lblATR As Label
    Friend WithEvents txtMaxPrice As TextBox
    Friend WithEvents lblMaxPrice As Label
    Friend WithEvents txtMinPrice As TextBox
    Friend WithEvents lblMinPrice As Label
    Friend WithEvents GroupBox4 As GroupBox
    Friend WithEvents chbAutoSelectStock As CheckBox
    Friend WithEvents txtManualStockList As TextBox
    Friend WithEvents lblManualStock As Label
    Friend WithEvents txtFutureMinCapital As TextBox
    Friend WithEvents lblFutureMinCapital As Label
    Friend WithEvents chbFuture As CheckBox
    Friend WithEvents chbCash As CheckBox
End Class
