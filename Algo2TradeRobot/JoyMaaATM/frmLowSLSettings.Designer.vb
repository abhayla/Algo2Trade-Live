<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class frmLowSLSettings
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmLowSLSettings))
        Me.txtNumberOfStock = New System.Windows.Forms.TextBox()
        Me.lblNumberOfStock = New System.Windows.Forms.Label()
        Me.txtMinVolume = New System.Windows.Forms.TextBox()
        Me.lblMinVolume = New System.Windows.Forms.Label()
        Me.txtATRPercentage = New System.Windows.Forms.TextBox()
        Me.lblATR = New System.Windows.Forms.Label()
        Me.txtMaxPrice = New System.Windows.Forms.TextBox()
        Me.lblMaxPrice = New System.Windows.Forms.Label()
        Me.lblMinPrice = New System.Windows.Forms.Label()
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
        Me.txtMinPrice = New System.Windows.Forms.TextBox()
        Me.opnFileSettings = New System.Windows.Forms.OpenFileDialog()
        Me.ImageList1 = New System.Windows.Forms.ImageList(Me.components)
        Me.btnJoyMaaATMStrategySettings = New System.Windows.Forms.Button()
        Me.chbAutoSelectStock = New System.Windows.Forms.CheckBox()
        Me.txtManualStockList = New System.Windows.Forms.TextBox()
        Me.lblManualStock = New System.Windows.Forms.Label()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.txtATRPeriod = New System.Windows.Forms.TextBox()
        Me.lblATRPeriod = New System.Windows.Forms.Label()
        Me.txtTargetMultiplier = New System.Windows.Forms.TextBox()
        Me.lblTargetMultiplier = New System.Windows.Forms.Label()
        Me.dtpckrEODExitTime = New System.Windows.Forms.DateTimePicker()
        Me.dtpckrLastTradeEntryTime = New System.Windows.Forms.DateTimePicker()
        Me.dtpckrTradeStartTime = New System.Windows.Forms.DateTimePicker()
        Me.lblEODExitTime = New System.Windows.Forms.Label()
        Me.lblLastTradeEntryTime = New System.Windows.Forms.Label()
        Me.lblTradeStartTime = New System.Windows.Forms.Label()
        Me.btnBrowse = New System.Windows.Forms.Button()
        Me.txtInstrumentDetalis = New System.Windows.Forms.TextBox()
        Me.lblInstrumentDetails = New System.Windows.Forms.Label()
        Me.txtSignalTimeFrame = New System.Windows.Forms.TextBox()
        Me.lblSignalTimeFrame = New System.Windows.Forms.Label()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.txtFutureMinCapital = New System.Windows.Forms.TextBox()
        Me.lblFutureMinCapital = New System.Windows.Forms.Label()
        Me.txtCashMaxSL = New System.Windows.Forms.TextBox()
        Me.lblCashMaxStoplossAmount = New System.Windows.Forms.Label()
        Me.chbFuture = New System.Windows.Forms.CheckBox()
        Me.chbCash = New System.Windows.Forms.CheckBox()
        Me.grpTelegram = New System.Windows.Forms.GroupBox()
        Me.txtTelegramChatIDForPL = New System.Windows.Forms.TextBox()
        Me.lblChatIDForPL = New System.Windows.Forms.Label()
        Me.txtTelegramChatID = New System.Windows.Forms.TextBox()
        Me.lblTelegramChatID = New System.Windows.Forms.Label()
        Me.txtTelegramAPI = New System.Windows.Forms.TextBox()
        Me.lblTelegramAPI = New System.Windows.Forms.Label()
        Me.GroupBox3.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.grpTelegram.SuspendLayout()
        Me.SuspendLayout()
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
        Me.GroupBox3.Location = New System.Drawing.Point(6, 264)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(843, 100)
        Me.GroupBox3.TabIndex = 39
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "Stock Selection Settings"
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
        'opnFileSettings
        '
        '
        'ImageList1
        '
        Me.ImageList1.ImageStream = CType(resources.GetObject("ImageList1.ImageStream"), System.Windows.Forms.ImageListStreamer)
        Me.ImageList1.TransparentColor = System.Drawing.Color.Transparent
        Me.ImageList1.Images.SetKeyName(0, "save-icon-36533.png")
        '
        'btnJoyMaaATMStrategySettings
        '
        Me.btnJoyMaaATMStrategySettings.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnJoyMaaATMStrategySettings.ImageKey = "save-icon-36533.png"
        Me.btnJoyMaaATMStrategySettings.ImageList = Me.ImageList1
        Me.btnJoyMaaATMStrategySettings.Location = New System.Drawing.Point(857, 9)
        Me.btnJoyMaaATMStrategySettings.Margin = New System.Windows.Forms.Padding(4)
        Me.btnJoyMaaATMStrategySettings.Name = "btnJoyMaaATMStrategySettings"
        Me.btnJoyMaaATMStrategySettings.Size = New System.Drawing.Size(112, 58)
        Me.btnJoyMaaATMStrategySettings.TabIndex = 36
        Me.btnJoyMaaATMStrategySettings.Text = "&Save"
        Me.btnJoyMaaATMStrategySettings.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnJoyMaaATMStrategySettings.UseVisualStyleBackColor = True
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
        Me.txtManualStockList.Location = New System.Drawing.Point(146, 132)
        Me.txtManualStockList.Margin = New System.Windows.Forms.Padding(4)
        Me.txtManualStockList.Multiline = True
        Me.txtManualStockList.Name = "txtManualStockList"
        Me.txtManualStockList.Size = New System.Drawing.Size(201, 116)
        Me.txtManualStockList.TabIndex = 38
        Me.txtManualStockList.Tag = "Signal Time Frame"
        '
        'lblManualStock
        '
        Me.lblManualStock.AutoSize = True
        Me.lblManualStock.Location = New System.Drawing.Point(6, 135)
        Me.lblManualStock.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblManualStock.Name = "lblManualStock"
        Me.lblManualStock.Size = New System.Drawing.Size(119, 17)
        Me.lblManualStock.TabIndex = 39
        Me.lblManualStock.Text = "Manual Stock List"
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.txtATRPeriod)
        Me.GroupBox1.Controls.Add(Me.lblATRPeriod)
        Me.GroupBox1.Controls.Add(Me.txtTargetMultiplier)
        Me.GroupBox1.Controls.Add(Me.lblTargetMultiplier)
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
        Me.GroupBox1.Location = New System.Drawing.Point(6, 0)
        Me.GroupBox1.Margin = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Padding = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Size = New System.Drawing.Size(477, 257)
        Me.GroupBox1.TabIndex = 37
        Me.GroupBox1.TabStop = False
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
        'txtTargetMultiplier
        '
        Me.txtTargetMultiplier.Location = New System.Drawing.Point(199, 194)
        Me.txtTargetMultiplier.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTargetMultiplier.Name = "txtTargetMultiplier"
        Me.txtTargetMultiplier.Size = New System.Drawing.Size(255, 22)
        Me.txtTargetMultiplier.TabIndex = 6
        Me.txtTargetMultiplier.Tag = "Max Profit Per Day"
        '
        'lblTargetMultiplier
        '
        Me.lblTargetMultiplier.AutoSize = True
        Me.lblTargetMultiplier.Location = New System.Drawing.Point(9, 198)
        Me.lblTargetMultiplier.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTargetMultiplier.Name = "lblTargetMultiplier"
        Me.lblTargetMultiplier.Size = New System.Drawing.Size(110, 17)
        Me.lblTargetMultiplier.TabIndex = 31
        Me.lblTargetMultiplier.Text = "Target Multiplier"
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
        'btnBrowse
        '
        Me.btnBrowse.Font = New System.Drawing.Font("Microsoft Sans Serif", 7.8!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnBrowse.Location = New System.Drawing.Point(428, 227)
        Me.btnBrowse.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.btnBrowse.Name = "btnBrowse"
        Me.btnBrowse.Size = New System.Drawing.Size(40, 23)
        Me.btnBrowse.TabIndex = 8
        Me.btnBrowse.Text = "..."
        Me.btnBrowse.UseVisualStyleBackColor = True
        '
        'txtInstrumentDetalis
        '
        Me.txtInstrumentDetalis.Location = New System.Drawing.Point(198, 228)
        Me.txtInstrumentDetalis.Margin = New System.Windows.Forms.Padding(4)
        Me.txtInstrumentDetalis.Name = "txtInstrumentDetalis"
        Me.txtInstrumentDetalis.ReadOnly = True
        Me.txtInstrumentDetalis.Size = New System.Drawing.Size(223, 22)
        Me.txtInstrumentDetalis.TabIndex = 15
        '
        'lblInstrumentDetails
        '
        Me.lblInstrumentDetails.AutoSize = True
        Me.lblInstrumentDetails.Location = New System.Drawing.Point(8, 231)
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
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.chbAutoSelectStock)
        Me.GroupBox2.Controls.Add(Me.txtManualStockList)
        Me.GroupBox2.Controls.Add(Me.lblManualStock)
        Me.GroupBox2.Controls.Add(Me.txtFutureMinCapital)
        Me.GroupBox2.Controls.Add(Me.lblFutureMinCapital)
        Me.GroupBox2.Controls.Add(Me.txtCashMaxSL)
        Me.GroupBox2.Controls.Add(Me.lblCashMaxStoplossAmount)
        Me.GroupBox2.Controls.Add(Me.chbFuture)
        Me.GroupBox2.Controls.Add(Me.chbCash)
        Me.GroupBox2.Location = New System.Drawing.Point(491, 1)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(358, 256)
        Me.GroupBox2.TabIndex = 38
        Me.GroupBox2.TabStop = False
        '
        'txtFutureMinCapital
        '
        Me.txtFutureMinCapital.Location = New System.Drawing.Point(146, 102)
        Me.txtFutureMinCapital.Margin = New System.Windows.Forms.Padding(4)
        Me.txtFutureMinCapital.Name = "txtFutureMinCapital"
        Me.txtFutureMinCapital.Size = New System.Drawing.Size(201, 22)
        Me.txtFutureMinCapital.TabIndex = 36
        Me.txtFutureMinCapital.Tag = "Signal Time Frame"
        '
        'lblFutureMinCapital
        '
        Me.lblFutureMinCapital.AutoSize = True
        Me.lblFutureMinCapital.Location = New System.Drawing.Point(6, 105)
        Me.lblFutureMinCapital.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblFutureMinCapital.Name = "lblFutureMinCapital"
        Me.lblFutureMinCapital.Size = New System.Drawing.Size(122, 17)
        Me.lblFutureMinCapital.TabIndex = 37
        Me.lblFutureMinCapital.Text = "Future Min Capital"
        '
        'txtCashMaxSL
        '
        Me.txtCashMaxSL.Location = New System.Drawing.Point(146, 73)
        Me.txtCashMaxSL.Margin = New System.Windows.Forms.Padding(4)
        Me.txtCashMaxSL.Name = "txtCashMaxSL"
        Me.txtCashMaxSL.Size = New System.Drawing.Size(201, 22)
        Me.txtCashMaxSL.TabIndex = 34
        Me.txtCashMaxSL.Tag = "Signal Time Frame"
        '
        'lblCashMaxStoplossAmount
        '
        Me.lblCashMaxStoplossAmount.AutoSize = True
        Me.lblCashMaxStoplossAmount.Location = New System.Drawing.Point(6, 76)
        Me.lblCashMaxStoplossAmount.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblCashMaxStoplossAmount.Name = "lblCashMaxStoplossAmount"
        Me.lblCashMaxStoplossAmount.Size = New System.Drawing.Size(90, 17)
        Me.lblCashMaxStoplossAmount.TabIndex = 35
        Me.lblCashMaxStoplossAmount.Text = "Cash Max SL"
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
        'grpTelegram
        '
        Me.grpTelegram.Controls.Add(Me.txtTelegramChatIDForPL)
        Me.grpTelegram.Controls.Add(Me.lblChatIDForPL)
        Me.grpTelegram.Controls.Add(Me.txtTelegramChatID)
        Me.grpTelegram.Controls.Add(Me.lblTelegramChatID)
        Me.grpTelegram.Controls.Add(Me.txtTelegramAPI)
        Me.grpTelegram.Controls.Add(Me.lblTelegramAPI)
        Me.grpTelegram.Location = New System.Drawing.Point(6, 370)
        Me.grpTelegram.Name = "grpTelegram"
        Me.grpTelegram.Size = New System.Drawing.Size(843, 64)
        Me.grpTelegram.TabIndex = 40
        Me.grpTelegram.TabStop = False
        Me.grpTelegram.Text = "Telegram Details"
        '
        'txtTelegramChatIDForPL
        '
        Me.txtTelegramChatIDForPL.Location = New System.Drawing.Point(660, 25)
        Me.txtTelegramChatIDForPL.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTelegramChatIDForPL.Name = "txtTelegramChatIDForPL"
        Me.txtTelegramChatIDForPL.Size = New System.Drawing.Size(172, 22)
        Me.txtTelegramChatIDForPL.TabIndex = 12
        '
        'lblChatIDForPL
        '
        Me.lblChatIDForPL.AutoSize = True
        Me.lblChatIDForPL.Location = New System.Drawing.Point(575, 28)
        Me.lblChatIDForPL.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblChatIDForPL.Name = "lblChatIDForPL"
        Me.lblChatIDForPL.Size = New System.Drawing.Size(75, 17)
        Me.lblChatIDForPL.TabIndex = 37
        Me.lblChatIDForPL.Text = "PL Chat ID"
        '
        'txtTelegramChatID
        '
        Me.txtTelegramChatID.Location = New System.Drawing.Point(419, 25)
        Me.txtTelegramChatID.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTelegramChatID.Name = "txtTelegramChatID"
        Me.txtTelegramChatID.Size = New System.Drawing.Size(141, 22)
        Me.txtTelegramChatID.TabIndex = 11
        '
        'lblTelegramChatID
        '
        Me.lblTelegramChatID.AutoSize = True
        Me.lblTelegramChatID.Location = New System.Drawing.Point(360, 28)
        Me.lblTelegramChatID.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTelegramChatID.Name = "lblTelegramChatID"
        Me.lblTelegramChatID.Size = New System.Drawing.Size(54, 17)
        Me.lblTelegramChatID.TabIndex = 35
        Me.lblTelegramChatID.Text = "Chat ID"
        '
        'txtTelegramAPI
        '
        Me.txtTelegramAPI.Location = New System.Drawing.Point(75, 25)
        Me.txtTelegramAPI.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTelegramAPI.Name = "txtTelegramAPI"
        Me.txtTelegramAPI.Size = New System.Drawing.Size(274, 22)
        Me.txtTelegramAPI.TabIndex = 10
        '
        'lblTelegramAPI
        '
        Me.lblTelegramAPI.AutoSize = True
        Me.lblTelegramAPI.Location = New System.Drawing.Point(10, 28)
        Me.lblTelegramAPI.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTelegramAPI.Name = "lblTelegramAPI"
        Me.lblTelegramAPI.Size = New System.Drawing.Size(57, 17)
        Me.lblTelegramAPI.TabIndex = 31
        Me.lblTelegramAPI.Text = "API Key"
        '
        'frmJoyMaaATMSettings
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(974, 439)
        Me.Controls.Add(Me.grpTelegram)
        Me.Controls.Add(Me.GroupBox3)
        Me.Controls.Add(Me.btnJoyMaaATMStrategySettings)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.GroupBox2)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "frmJoyMaaATMSettings"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Joy Maa ATM Strategy - Settings"
        Me.GroupBox3.ResumeLayout(False)
        Me.GroupBox3.PerformLayout()
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.grpTelegram.ResumeLayout(False)
        Me.grpTelegram.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents txtNumberOfStock As TextBox
    Friend WithEvents lblNumberOfStock As Label
    Friend WithEvents txtMinVolume As TextBox
    Friend WithEvents lblMinVolume As Label
    Friend WithEvents txtATRPercentage As TextBox
    Friend WithEvents lblATR As Label
    Friend WithEvents txtMaxPrice As TextBox
    Friend WithEvents lblMaxPrice As Label
    Friend WithEvents lblMinPrice As Label
    Friend WithEvents GroupBox3 As GroupBox
    Friend WithEvents txtMinPrice As TextBox
    Friend WithEvents opnFileSettings As OpenFileDialog
    Friend WithEvents ImageList1 As ImageList
    Friend WithEvents btnJoyMaaATMStrategySettings As Button
    Friend WithEvents chbAutoSelectStock As CheckBox
    Friend WithEvents txtManualStockList As TextBox
    Friend WithEvents lblManualStock As Label
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents txtATRPeriod As TextBox
    Friend WithEvents lblATRPeriod As Label
    Friend WithEvents txtTargetMultiplier As TextBox
    Friend WithEvents lblTargetMultiplier As Label
    Friend WithEvents dtpckrEODExitTime As DateTimePicker
    Friend WithEvents dtpckrLastTradeEntryTime As DateTimePicker
    Friend WithEvents dtpckrTradeStartTime As DateTimePicker
    Friend WithEvents lblEODExitTime As Label
    Friend WithEvents lblLastTradeEntryTime As Label
    Friend WithEvents lblTradeStartTime As Label
    Friend WithEvents btnBrowse As Button
    Friend WithEvents txtInstrumentDetalis As TextBox
    Friend WithEvents lblInstrumentDetails As Label
    Friend WithEvents txtSignalTimeFrame As TextBox
    Friend WithEvents lblSignalTimeFrame As Label
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents txtFutureMinCapital As TextBox
    Friend WithEvents lblFutureMinCapital As Label
    Friend WithEvents txtCashMaxSL As TextBox
    Friend WithEvents lblCashMaxStoplossAmount As Label
    Friend WithEvents chbFuture As CheckBox
    Friend WithEvents chbCash As CheckBox
    Friend WithEvents grpTelegram As GroupBox
    Friend WithEvents txtTelegramChatIDForPL As TextBox
    Friend WithEvents lblChatIDForPL As Label
    Friend WithEvents txtTelegramChatID As TextBox
    Friend WithEvents lblTelegramChatID As Label
    Friend WithEvents txtTelegramAPI As TextBox
    Friend WithEvents lblTelegramAPI As Label
End Class
