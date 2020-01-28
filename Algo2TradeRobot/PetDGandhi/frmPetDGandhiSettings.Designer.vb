<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class frmPetDGandhiSettings
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmPetDGandhiSettings))
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
        Me.rdbFuture = New System.Windows.Forms.RadioButton()
        Me.rdbCash = New System.Windows.Forms.RadioButton()
        Me.chbAutoSelectStock = New System.Windows.Forms.CheckBox()
        Me.txtBlankCandlePercentage = New System.Windows.Forms.TextBox()
        Me.lblBlankCandlePercentage = New System.Windows.Forms.Label()
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
        Me.btnSavePetDGandhiSettings = New System.Windows.Forms.Button()
        Me.ImageList1 = New System.Windows.Forms.ImageList(Me.components)
        Me.txtMaxProfitPerTrade = New System.Windows.Forms.TextBox()
        Me.lblMaxProfitPerTrade = New System.Windows.Forms.Label()
        Me.txtStockMaxProfitPerDay = New System.Windows.Forms.TextBox()
        Me.lblStockMaxProfitPerDay = New System.Windows.Forms.Label()
        Me.txtStockMaxLossPerDay = New System.Windows.Forms.TextBox()
        Me.lblStockMaxLossPerDay = New System.Windows.Forms.Label()
        Me.opnFileSettings = New System.Windows.Forms.OpenFileDialog()
        Me.txtMaxProfitPerDay = New System.Windows.Forms.TextBox()
        Me.lblMaxProfitPerDay = New System.Windows.Forms.Label()
        Me.txtMaxLossPerDay = New System.Windows.Forms.TextBox()
        Me.lblMaxLossPerDay = New System.Windows.Forms.Label()
        Me.dtpckrEODExitTime = New System.Windows.Forms.DateTimePicker()
        Me.dtpckrLastTradeEntryTime = New System.Windows.Forms.DateTimePicker()
        Me.dtpckrTradeStartTime = New System.Windows.Forms.DateTimePicker()
        Me.lblEODExitTime = New System.Windows.Forms.Label()
        Me.lblLastTradeEntryTime = New System.Windows.Forms.Label()
        Me.lblTradeStartTime = New System.Windows.Forms.Label()
        Me.btnBrowse = New System.Windows.Forms.Button()
        Me.txtInstrumentDetalis = New System.Windows.Forms.TextBox()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.txtNumberOfTradePerStock = New System.Windows.Forms.TextBox()
        Me.lblNumberOfTradePerStock = New System.Windows.Forms.Label()
        Me.lblInstrumentDetails = New System.Windows.Forms.Label()
        Me.txtSignalTimeFrame = New System.Windows.Forms.TextBox()
        Me.lblSignalTimeFrame = New System.Windows.Forms.Label()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.txtSupertrendMultiplier = New System.Windows.Forms.TextBox()
        Me.lblSupertrendMultiplier = New System.Windows.Forms.Label()
        Me.txtSupertrendPeriod = New System.Windows.Forms.TextBox()
        Me.lblSupertrendPeriod = New System.Windows.Forms.Label()
        Me.GroupBox3.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.rdbFuture)
        Me.GroupBox3.Controls.Add(Me.rdbCash)
        Me.GroupBox3.Controls.Add(Me.chbAutoSelectStock)
        Me.GroupBox3.Controls.Add(Me.txtBlankCandlePercentage)
        Me.GroupBox3.Controls.Add(Me.lblBlankCandlePercentage)
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
        Me.GroupBox3.Location = New System.Drawing.Point(452, 5)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(358, 255)
        Me.GroupBox3.TabIndex = 42
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "Stock Selection Settings"
        '
        'rdbFuture
        '
        Me.rdbFuture.AutoSize = True
        Me.rdbFuture.Checked = True
        Me.rdbFuture.Location = New System.Drawing.Point(77, 48)
        Me.rdbFuture.Name = "rdbFuture"
        Me.rdbFuture.Size = New System.Drawing.Size(70, 21)
        Me.rdbFuture.TabIndex = 48
        Me.rdbFuture.TabStop = True
        Me.rdbFuture.Text = "Future"
        Me.rdbFuture.UseVisualStyleBackColor = True
        '
        'rdbCash
        '
        Me.rdbCash.AutoSize = True
        Me.rdbCash.Location = New System.Drawing.Point(14, 48)
        Me.rdbCash.Name = "rdbCash"
        Me.rdbCash.Size = New System.Drawing.Size(61, 21)
        Me.rdbCash.TabIndex = 47
        Me.rdbCash.Text = "Cash"
        Me.rdbCash.UseVisualStyleBackColor = True
        '
        'chbAutoSelectStock
        '
        Me.chbAutoSelectStock.AutoSize = True
        Me.chbAutoSelectStock.Location = New System.Drawing.Point(13, 20)
        Me.chbAutoSelectStock.Name = "chbAutoSelectStock"
        Me.chbAutoSelectStock.Size = New System.Drawing.Size(141, 21)
        Me.chbAutoSelectStock.TabIndex = 46
        Me.chbAutoSelectStock.Text = "Auto Select Stock"
        Me.chbAutoSelectStock.UseVisualStyleBackColor = True
        '
        'txtBlankCandlePercentage
        '
        Me.txtBlankCandlePercentage.Location = New System.Drawing.Point(146, 194)
        Me.txtBlankCandlePercentage.Margin = New System.Windows.Forms.Padding(4)
        Me.txtBlankCandlePercentage.Name = "txtBlankCandlePercentage"
        Me.txtBlankCandlePercentage.Size = New System.Drawing.Size(201, 22)
        Me.txtBlankCandlePercentage.TabIndex = 31
        Me.txtBlankCandlePercentage.Tag = "Number Of Stock"
        '
        'lblBlankCandlePercentage
        '
        Me.lblBlankCandlePercentage.AutoSize = True
        Me.lblBlankCandlePercentage.Location = New System.Drawing.Point(10, 197)
        Me.lblBlankCandlePercentage.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblBlankCandlePercentage.Name = "lblBlankCandlePercentage"
        Me.lblBlankCandlePercentage.Size = New System.Drawing.Size(107, 17)
        Me.lblBlankCandlePercentage.TabIndex = 45
        Me.lblBlankCandlePercentage.Text = "Blank Candle %"
        '
        'txtNumberOfStock
        '
        Me.txtNumberOfStock.Location = New System.Drawing.Point(146, 224)
        Me.txtNumberOfStock.Margin = New System.Windows.Forms.Padding(4)
        Me.txtNumberOfStock.Name = "txtNumberOfStock"
        Me.txtNumberOfStock.Size = New System.Drawing.Size(201, 22)
        Me.txtNumberOfStock.TabIndex = 32
        Me.txtNumberOfStock.Tag = "Number Of Stock"
        '
        'lblNumberOfStock
        '
        Me.lblNumberOfStock.AutoSize = True
        Me.lblNumberOfStock.Location = New System.Drawing.Point(9, 227)
        Me.lblNumberOfStock.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblNumberOfStock.Name = "lblNumberOfStock"
        Me.lblNumberOfStock.Size = New System.Drawing.Size(116, 17)
        Me.lblNumberOfStock.TabIndex = 43
        Me.lblNumberOfStock.Text = "Number Of Stock"
        '
        'txtMinVolume
        '
        Me.txtMinVolume.Location = New System.Drawing.Point(146, 165)
        Me.txtMinVolume.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMinVolume.Name = "txtMinVolume"
        Me.txtMinVolume.Size = New System.Drawing.Size(201, 22)
        Me.txtMinVolume.TabIndex = 30
        Me.txtMinVolume.Tag = "Min Volume"
        '
        'lblMinVolume
        '
        Me.lblMinVolume.AutoSize = True
        Me.lblMinVolume.Location = New System.Drawing.Point(9, 165)
        Me.lblMinVolume.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMinVolume.Name = "lblMinVolume"
        Me.lblMinVolume.Size = New System.Drawing.Size(81, 17)
        Me.lblMinVolume.TabIndex = 41
        Me.lblMinVolume.Text = "Min Volume"
        '
        'txtATRPercentage
        '
        Me.txtATRPercentage.Location = New System.Drawing.Point(146, 135)
        Me.txtATRPercentage.Margin = New System.Windows.Forms.Padding(4)
        Me.txtATRPercentage.Name = "txtATRPercentage"
        Me.txtATRPercentage.Size = New System.Drawing.Size(201, 22)
        Me.txtATRPercentage.TabIndex = 29
        Me.txtATRPercentage.Tag = "ATR %"
        '
        'lblATR
        '
        Me.lblATR.AutoSize = True
        Me.lblATR.Location = New System.Drawing.Point(9, 138)
        Me.lblATR.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblATR.Name = "lblATR"
        Me.lblATR.Size = New System.Drawing.Size(52, 17)
        Me.lblATR.TabIndex = 39
        Me.lblATR.Text = "ATR %"
        '
        'txtMaxPrice
        '
        Me.txtMaxPrice.Location = New System.Drawing.Point(146, 106)
        Me.txtMaxPrice.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxPrice.Name = "txtMaxPrice"
        Me.txtMaxPrice.Size = New System.Drawing.Size(201, 22)
        Me.txtMaxPrice.TabIndex = 28
        Me.txtMaxPrice.Tag = "Max Price"
        '
        'lblMaxPrice
        '
        Me.lblMaxPrice.AutoSize = True
        Me.lblMaxPrice.Location = New System.Drawing.Point(9, 110)
        Me.lblMaxPrice.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxPrice.Name = "lblMaxPrice"
        Me.lblMaxPrice.Size = New System.Drawing.Size(69, 17)
        Me.lblMaxPrice.TabIndex = 37
        Me.lblMaxPrice.Text = "Max Price"
        '
        'txtMinPrice
        '
        Me.txtMinPrice.Location = New System.Drawing.Point(146, 76)
        Me.txtMinPrice.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMinPrice.Name = "txtMinPrice"
        Me.txtMinPrice.Size = New System.Drawing.Size(201, 22)
        Me.txtMinPrice.TabIndex = 27
        Me.txtMinPrice.Tag = "Min Price"
        '
        'lblMinPrice
        '
        Me.lblMinPrice.AutoSize = True
        Me.lblMinPrice.Location = New System.Drawing.Point(9, 79)
        Me.lblMinPrice.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMinPrice.Name = "lblMinPrice"
        Me.lblMinPrice.Size = New System.Drawing.Size(66, 17)
        Me.lblMinPrice.TabIndex = 35
        Me.lblMinPrice.Text = "Min Price"
        '
        'btnSavePetDGandhiSettings
        '
        Me.btnSavePetDGandhiSettings.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnSavePetDGandhiSettings.ImageKey = "save-icon-36533.png"
        Me.btnSavePetDGandhiSettings.ImageList = Me.ImageList1
        Me.btnSavePetDGandhiSettings.Location = New System.Drawing.Point(829, 12)
        Me.btnSavePetDGandhiSettings.Margin = New System.Windows.Forms.Padding(4)
        Me.btnSavePetDGandhiSettings.Name = "btnSavePetDGandhiSettings"
        Me.btnSavePetDGandhiSettings.Size = New System.Drawing.Size(112, 58)
        Me.btnSavePetDGandhiSettings.TabIndex = 40
        Me.btnSavePetDGandhiSettings.Text = "&Save"
        Me.btnSavePetDGandhiSettings.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnSavePetDGandhiSettings.UseVisualStyleBackColor = True
        '
        'ImageList1
        '
        Me.ImageList1.ImageStream = CType(resources.GetObject("ImageList1.ImageStream"), System.Windows.Forms.ImageListStreamer)
        Me.ImageList1.TransparentColor = System.Drawing.Color.Transparent
        Me.ImageList1.Images.SetKeyName(0, "save-icon-36533.png")
        '
        'txtMaxProfitPerTrade
        '
        Me.txtMaxProfitPerTrade.Location = New System.Drawing.Point(174, 197)
        Me.txtMaxProfitPerTrade.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxProfitPerTrade.Name = "txtMaxProfitPerTrade"
        Me.txtMaxProfitPerTrade.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxProfitPerTrade.TabIndex = 42
        Me.txtMaxProfitPerTrade.Tag = "Max Profit Per Day"
        '
        'lblMaxProfitPerTrade
        '
        Me.lblMaxProfitPerTrade.AutoSize = True
        Me.lblMaxProfitPerTrade.Location = New System.Drawing.Point(8, 200)
        Me.lblMaxProfitPerTrade.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxProfitPerTrade.Name = "lblMaxProfitPerTrade"
        Me.lblMaxProfitPerTrade.Size = New System.Drawing.Size(138, 17)
        Me.lblMaxProfitPerTrade.TabIndex = 43
        Me.lblMaxProfitPerTrade.Text = "Max Profit Per Trade"
        '
        'txtStockMaxProfitPerDay
        '
        Me.txtStockMaxProfitPerDay.Location = New System.Drawing.Point(174, 265)
        Me.txtStockMaxProfitPerDay.Margin = New System.Windows.Forms.Padding(4)
        Me.txtStockMaxProfitPerDay.Name = "txtStockMaxProfitPerDay"
        Me.txtStockMaxProfitPerDay.Size = New System.Drawing.Size(255, 22)
        Me.txtStockMaxProfitPerDay.TabIndex = 39
        '
        'lblStockMaxProfitPerDay
        '
        Me.lblStockMaxProfitPerDay.AutoSize = True
        Me.lblStockMaxProfitPerDay.Location = New System.Drawing.Point(8, 269)
        Me.lblStockMaxProfitPerDay.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblStockMaxProfitPerDay.Name = "lblStockMaxProfitPerDay"
        Me.lblStockMaxProfitPerDay.Size = New System.Drawing.Size(164, 17)
        Me.lblStockMaxProfitPerDay.TabIndex = 41
        Me.lblStockMaxProfitPerDay.Text = "Stock Max Profit Per Day"
        '
        'txtStockMaxLossPerDay
        '
        Me.txtStockMaxLossPerDay.Location = New System.Drawing.Point(174, 230)
        Me.txtStockMaxLossPerDay.Margin = New System.Windows.Forms.Padding(4)
        Me.txtStockMaxLossPerDay.Name = "txtStockMaxLossPerDay"
        Me.txtStockMaxLossPerDay.Size = New System.Drawing.Size(255, 22)
        Me.txtStockMaxLossPerDay.TabIndex = 38
        '
        'lblStockMaxLossPerDay
        '
        Me.lblStockMaxLossPerDay.AutoSize = True
        Me.lblStockMaxLossPerDay.Location = New System.Drawing.Point(8, 234)
        Me.lblStockMaxLossPerDay.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblStockMaxLossPerDay.Name = "lblStockMaxLossPerDay"
        Me.lblStockMaxLossPerDay.Size = New System.Drawing.Size(161, 17)
        Me.lblStockMaxLossPerDay.TabIndex = 40
        Me.lblStockMaxLossPerDay.Text = "Stock Max Loss Per Day"
        '
        'opnFileSettings
        '
        '
        'txtMaxProfitPerDay
        '
        Me.txtMaxProfitPerDay.Location = New System.Drawing.Point(174, 331)
        Me.txtMaxProfitPerDay.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxProfitPerDay.Name = "txtMaxProfitPerDay"
        Me.txtMaxProfitPerDay.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxProfitPerDay.TabIndex = 8
        '
        'lblMaxProfitPerDay
        '
        Me.lblMaxProfitPerDay.AutoSize = True
        Me.lblMaxProfitPerDay.Location = New System.Drawing.Point(8, 335)
        Me.lblMaxProfitPerDay.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxProfitPerDay.Name = "lblMaxProfitPerDay"
        Me.lblMaxProfitPerDay.Size = New System.Drawing.Size(125, 17)
        Me.lblMaxProfitPerDay.TabIndex = 27
        Me.lblMaxProfitPerDay.Text = "Max Profit Per Day"
        '
        'txtMaxLossPerDay
        '
        Me.txtMaxLossPerDay.Location = New System.Drawing.Point(174, 298)
        Me.txtMaxLossPerDay.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxLossPerDay.Name = "txtMaxLossPerDay"
        Me.txtMaxLossPerDay.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxLossPerDay.TabIndex = 7
        '
        'lblMaxLossPerDay
        '
        Me.lblMaxLossPerDay.AutoSize = True
        Me.lblMaxLossPerDay.Location = New System.Drawing.Point(8, 302)
        Me.lblMaxLossPerDay.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxLossPerDay.Name = "lblMaxLossPerDay"
        Me.lblMaxLossPerDay.Size = New System.Drawing.Size(122, 17)
        Me.lblMaxLossPerDay.TabIndex = 25
        Me.lblMaxLossPerDay.Text = "Max Loss Per Day"
        '
        'dtpckrEODExitTime
        '
        Me.dtpckrEODExitTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrEODExitTime.Location = New System.Drawing.Point(175, 126)
        Me.dtpckrEODExitTime.Name = "dtpckrEODExitTime"
        Me.dtpckrEODExitTime.ShowUpDown = True
        Me.dtpckrEODExitTime.Size = New System.Drawing.Size(255, 22)
        Me.dtpckrEODExitTime.TabIndex = 4
        Me.dtpckrEODExitTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'dtpckrLastTradeEntryTime
        '
        Me.dtpckrLastTradeEntryTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrLastTradeEntryTime.Location = New System.Drawing.Point(176, 91)
        Me.dtpckrLastTradeEntryTime.Name = "dtpckrLastTradeEntryTime"
        Me.dtpckrLastTradeEntryTime.ShowUpDown = True
        Me.dtpckrLastTradeEntryTime.Size = New System.Drawing.Size(255, 22)
        Me.dtpckrLastTradeEntryTime.TabIndex = 3
        Me.dtpckrLastTradeEntryTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'dtpckrTradeStartTime
        '
        Me.dtpckrTradeStartTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrTradeStartTime.Location = New System.Drawing.Point(175, 54)
        Me.dtpckrTradeStartTime.Name = "dtpckrTradeStartTime"
        Me.dtpckrTradeStartTime.ShowUpDown = True
        Me.dtpckrTradeStartTime.Size = New System.Drawing.Size(255, 22)
        Me.dtpckrTradeStartTime.TabIndex = 2
        Me.dtpckrTradeStartTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'lblEODExitTime
        '
        Me.lblEODExitTime.AutoSize = True
        Me.lblEODExitTime.Location = New System.Drawing.Point(9, 127)
        Me.lblEODExitTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblEODExitTime.Name = "lblEODExitTime"
        Me.lblEODExitTime.Size = New System.Drawing.Size(99, 17)
        Me.lblEODExitTime.TabIndex = 23
        Me.lblEODExitTime.Text = "EOD Exit Time"
        '
        'lblLastTradeEntryTime
        '
        Me.lblLastTradeEntryTime.AutoSize = True
        Me.lblLastTradeEntryTime.Location = New System.Drawing.Point(9, 92)
        Me.lblLastTradeEntryTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblLastTradeEntryTime.Name = "lblLastTradeEntryTime"
        Me.lblLastTradeEntryTime.Size = New System.Drawing.Size(149, 17)
        Me.lblLastTradeEntryTime.TabIndex = 21
        Me.lblLastTradeEntryTime.Text = "Last Trade Entry Time"
        '
        'lblTradeStartTime
        '
        Me.lblTradeStartTime.AutoSize = True
        Me.lblTradeStartTime.Location = New System.Drawing.Point(9, 56)
        Me.lblTradeStartTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTradeStartTime.Name = "lblTradeStartTime"
        Me.lblTradeStartTime.Size = New System.Drawing.Size(115, 17)
        Me.lblTradeStartTime.TabIndex = 19
        Me.lblTradeStartTime.Text = "Trade Start Time"
        '
        'btnBrowse
        '
        Me.btnBrowse.Font = New System.Drawing.Font("Microsoft Sans Serif", 7.8!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnBrowse.Location = New System.Drawing.Point(390, 363)
        Me.btnBrowse.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.btnBrowse.Name = "btnBrowse"
        Me.btnBrowse.Size = New System.Drawing.Size(40, 23)
        Me.btnBrowse.TabIndex = 9
        Me.btnBrowse.Text = "..."
        Me.btnBrowse.UseVisualStyleBackColor = True
        '
        'txtInstrumentDetalis
        '
        Me.txtInstrumentDetalis.Location = New System.Drawing.Point(174, 364)
        Me.txtInstrumentDetalis.Margin = New System.Windows.Forms.Padding(4)
        Me.txtInstrumentDetalis.Name = "txtInstrumentDetalis"
        Me.txtInstrumentDetalis.ReadOnly = True
        Me.txtInstrumentDetalis.Size = New System.Drawing.Size(215, 22)
        Me.txtInstrumentDetalis.TabIndex = 11
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.txtMaxProfitPerTrade)
        Me.GroupBox1.Controls.Add(Me.lblMaxProfitPerTrade)
        Me.GroupBox1.Controls.Add(Me.txtStockMaxProfitPerDay)
        Me.GroupBox1.Controls.Add(Me.lblStockMaxProfitPerDay)
        Me.GroupBox1.Controls.Add(Me.txtStockMaxLossPerDay)
        Me.GroupBox1.Controls.Add(Me.lblStockMaxLossPerDay)
        Me.GroupBox1.Controls.Add(Me.txtNumberOfTradePerStock)
        Me.GroupBox1.Controls.Add(Me.lblNumberOfTradePerStock)
        Me.GroupBox1.Controls.Add(Me.txtMaxProfitPerDay)
        Me.GroupBox1.Controls.Add(Me.lblMaxProfitPerDay)
        Me.GroupBox1.Controls.Add(Me.txtMaxLossPerDay)
        Me.GroupBox1.Controls.Add(Me.lblMaxLossPerDay)
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
        Me.GroupBox1.Location = New System.Drawing.Point(5, 5)
        Me.GroupBox1.Margin = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Padding = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Size = New System.Drawing.Size(437, 397)
        Me.GroupBox1.TabIndex = 41
        Me.GroupBox1.TabStop = False
        '
        'txtNumberOfTradePerStock
        '
        Me.txtNumberOfTradePerStock.Location = New System.Drawing.Point(174, 163)
        Me.txtNumberOfTradePerStock.Margin = New System.Windows.Forms.Padding(4)
        Me.txtNumberOfTradePerStock.Name = "txtNumberOfTradePerStock"
        Me.txtNumberOfTradePerStock.Size = New System.Drawing.Size(255, 22)
        Me.txtNumberOfTradePerStock.TabIndex = 5
        Me.txtNumberOfTradePerStock.Tag = "Max Loss Per Day"
        '
        'lblNumberOfTradePerStock
        '
        Me.lblNumberOfTradePerStock.AutoSize = True
        Me.lblNumberOfTradePerStock.Location = New System.Drawing.Point(9, 166)
        Me.lblNumberOfTradePerStock.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblNumberOfTradePerStock.Name = "lblNumberOfTradePerStock"
        Me.lblNumberOfTradePerStock.Size = New System.Drawing.Size(152, 17)
        Me.lblNumberOfTradePerStock.TabIndex = 36
        Me.lblNumberOfTradePerStock.Text = "No Of Trade Per Stock"
        '
        'lblInstrumentDetails
        '
        Me.lblInstrumentDetails.AutoSize = True
        Me.lblInstrumentDetails.Location = New System.Drawing.Point(8, 367)
        Me.lblInstrumentDetails.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblInstrumentDetails.Name = "lblInstrumentDetails"
        Me.lblInstrumentDetails.Size = New System.Drawing.Size(121, 17)
        Me.lblInstrumentDetails.TabIndex = 8
        Me.lblInstrumentDetails.Text = "Instrument Details"
        '
        'txtSignalTimeFrame
        '
        Me.txtSignalTimeFrame.Location = New System.Drawing.Point(175, 17)
        Me.txtSignalTimeFrame.Margin = New System.Windows.Forms.Padding(4)
        Me.txtSignalTimeFrame.Name = "txtSignalTimeFrame"
        Me.txtSignalTimeFrame.Size = New System.Drawing.Size(255, 22)
        Me.txtSignalTimeFrame.TabIndex = 1
        '
        'lblSignalTimeFrame
        '
        Me.lblSignalTimeFrame.AutoSize = True
        Me.lblSignalTimeFrame.Location = New System.Drawing.Point(9, 20)
        Me.lblSignalTimeFrame.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblSignalTimeFrame.Name = "lblSignalTimeFrame"
        Me.lblSignalTimeFrame.Size = New System.Drawing.Size(158, 17)
        Me.lblSignalTimeFrame.TabIndex = 3
        Me.lblSignalTimeFrame.Text = "Signal Time Frame(min)"
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.txtSupertrendMultiplier)
        Me.GroupBox2.Controls.Add(Me.lblSupertrendMultiplier)
        Me.GroupBox2.Controls.Add(Me.txtSupertrendPeriod)
        Me.GroupBox2.Controls.Add(Me.lblSupertrendPeriod)
        Me.GroupBox2.Location = New System.Drawing.Point(452, 265)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(358, 137)
        Me.GroupBox2.TabIndex = 43
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Indicator Settings"
        '
        'txtSupertrendMultiplier
        '
        Me.txtSupertrendMultiplier.Location = New System.Drawing.Point(156, 54)
        Me.txtSupertrendMultiplier.Margin = New System.Windows.Forms.Padding(4)
        Me.txtSupertrendMultiplier.Name = "txtSupertrendMultiplier"
        Me.txtSupertrendMultiplier.Size = New System.Drawing.Size(191, 22)
        Me.txtSupertrendMultiplier.TabIndex = 34
        '
        'lblSupertrendMultiplier
        '
        Me.lblSupertrendMultiplier.AutoSize = True
        Me.lblSupertrendMultiplier.Location = New System.Drawing.Point(9, 58)
        Me.lblSupertrendMultiplier.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblSupertrendMultiplier.Name = "lblSupertrendMultiplier"
        Me.lblSupertrendMultiplier.Size = New System.Drawing.Size(139, 17)
        Me.lblSupertrendMultiplier.TabIndex = 37
        Me.lblSupertrendMultiplier.Text = "Supertrend Multiplier"
        '
        'txtSupertrendPeriod
        '
        Me.txtSupertrendPeriod.Location = New System.Drawing.Point(156, 20)
        Me.txtSupertrendPeriod.Margin = New System.Windows.Forms.Padding(4)
        Me.txtSupertrendPeriod.Name = "txtSupertrendPeriod"
        Me.txtSupertrendPeriod.Size = New System.Drawing.Size(191, 22)
        Me.txtSupertrendPeriod.TabIndex = 33
        '
        'lblSupertrendPeriod
        '
        Me.lblSupertrendPeriod.AutoSize = True
        Me.lblSupertrendPeriod.Location = New System.Drawing.Point(9, 24)
        Me.lblSupertrendPeriod.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblSupertrendPeriod.Name = "lblSupertrendPeriod"
        Me.lblSupertrendPeriod.Size = New System.Drawing.Size(124, 17)
        Me.lblSupertrendPeriod.TabIndex = 36
        Me.lblSupertrendPeriod.Text = "Supertrend Period"
        '
        'frmPetDGandhiSettings
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(947, 408)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.GroupBox3)
        Me.Controls.Add(Me.btnSavePetDGandhiSettings)
        Me.Controls.Add(Me.GroupBox1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "frmPetDGandhiSettings"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Pet-D Gandhi Strategy - Settings"
        Me.GroupBox3.ResumeLayout(False)
        Me.GroupBox3.PerformLayout()
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents GroupBox3 As GroupBox
    Friend WithEvents rdbFuture As RadioButton
    Friend WithEvents rdbCash As RadioButton
    Friend WithEvents chbAutoSelectStock As CheckBox
    Friend WithEvents txtBlankCandlePercentage As TextBox
    Friend WithEvents lblBlankCandlePercentage As Label
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
    Friend WithEvents btnSavePetDGandhiSettings As Button
    Friend WithEvents ImageList1 As ImageList
    Friend WithEvents txtMaxProfitPerTrade As TextBox
    Friend WithEvents lblMaxProfitPerTrade As Label
    Friend WithEvents txtStockMaxProfitPerDay As TextBox
    Friend WithEvents lblStockMaxProfitPerDay As Label
    Friend WithEvents txtStockMaxLossPerDay As TextBox
    Friend WithEvents lblStockMaxLossPerDay As Label
    Friend WithEvents opnFileSettings As OpenFileDialog
    Friend WithEvents txtMaxProfitPerDay As TextBox
    Friend WithEvents lblMaxProfitPerDay As Label
    Friend WithEvents txtMaxLossPerDay As TextBox
    Friend WithEvents lblMaxLossPerDay As Label
    Friend WithEvents dtpckrEODExitTime As DateTimePicker
    Friend WithEvents dtpckrLastTradeEntryTime As DateTimePicker
    Friend WithEvents dtpckrTradeStartTime As DateTimePicker
    Friend WithEvents lblEODExitTime As Label
    Friend WithEvents lblLastTradeEntryTime As Label
    Friend WithEvents lblTradeStartTime As Label
    Friend WithEvents btnBrowse As Button
    Friend WithEvents txtInstrumentDetalis As TextBox
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents lblInstrumentDetails As Label
    Friend WithEvents txtSignalTimeFrame As TextBox
    Friend WithEvents lblSignalTimeFrame As Label
    Friend WithEvents txtNumberOfTradePerStock As TextBox
    Friend WithEvents lblNumberOfTradePerStock As Label
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents txtSupertrendMultiplier As TextBox
    Friend WithEvents lblSupertrendMultiplier As Label
    Friend WithEvents txtSupertrendPeriod As TextBox
    Friend WithEvents lblSupertrendPeriod As Label
End Class
