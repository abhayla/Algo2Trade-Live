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
        Me.txtMaxProfitPerStockMultiplier = New System.Windows.Forms.TextBox()
        Me.lblMaxProfitPerStockMultiplier = New System.Windows.Forms.Label()
        Me.txtMaxLossPerStockMultiplier = New System.Windows.Forms.TextBox()
        Me.lblMaxLossPerStockMultiplier = New System.Windows.Forms.Label()
        Me.txtPinbarTailPercentage = New System.Windows.Forms.TextBox()
        Me.lblPinbarTailPercentage = New System.Windows.Forms.Label()
        Me.txtMinLossPercentagePerTrade = New System.Windows.Forms.TextBox()
        Me.lblMinLossPercentagePerTrade = New System.Windows.Forms.Label()
        Me.txtMaxLossPerTradeMultiplier = New System.Windows.Forms.TextBox()
        Me.lblMaxLossPerTrade = New System.Windows.Forms.Label()
        Me.txtTargetMultiplier = New System.Windows.Forms.TextBox()
        Me.lblTargetMultiplier = New System.Windows.Forms.Label()
        Me.txtNumberOfTradePerStock = New System.Windows.Forms.TextBox()
        Me.lblNumberOfTradePerStock = New System.Windows.Forms.Label()
        Me.txtATRPeriod = New System.Windows.Forms.TextBox()
        Me.lblATRPeriod = New System.Windows.Forms.Label()
        Me.lblInstrumentDetails = New System.Windows.Forms.Label()
        Me.txtSignalTimeFrame = New System.Windows.Forms.TextBox()
        Me.lblSignalTimeFrame = New System.Windows.Forms.Label()
        Me.ImageList1 = New System.Windows.Forms.ImageList(Me.components)
        Me.btnSavePetDGandhiSettings = New System.Windows.Forms.Button()
        Me.opnFileSettings = New System.Windows.Forms.OpenFileDialog()
        Me.grpTelegram = New System.Windows.Forms.GroupBox()
        Me.txtTelegramSignalChatID = New System.Windows.Forms.TextBox()
        Me.lblTelegramSignalChatID = New System.Windows.Forms.Label()
        Me.txtTelegramTargetChatID = New System.Windows.Forms.TextBox()
        Me.lblTelegramTargetChatID = New System.Windows.Forms.Label()
        Me.txtTelegramMTMChatID = New System.Windows.Forms.TextBox()
        Me.lblMTMChatID = New System.Windows.Forms.Label()
        Me.txtTelegramTradeChatID = New System.Windows.Forms.TextBox()
        Me.lblTelegramTradeChatID = New System.Windows.Forms.Label()
        Me.txtTelegramAPI = New System.Windows.Forms.TextBox()
        Me.lblTelegramAPI = New System.Windows.Forms.Label()
        Me.GroupBox4 = New System.Windows.Forms.GroupBox()
        Me.chbAllowToIncreaseQuantity = New System.Windows.Forms.CheckBox()
        Me.chbAutoSelectStock = New System.Windows.Forms.CheckBox()
        Me.txtMinCapitalPerStock = New System.Windows.Forms.TextBox()
        Me.lblMinCapitalPerStock = New System.Windows.Forms.Label()
        Me.chbFuture = New System.Windows.Forms.CheckBox()
        Me.chbCash = New System.Windows.Forms.CheckBox()
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
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
        Me.txtMaxCapital = New System.Windows.Forms.TextBox()
        Me.lblMaxCapital = New System.Windows.Forms.Label()
        Me.GroupBox1.SuspendLayout()
        Me.grpTelegram.SuspendLayout()
        Me.GroupBox4.SuspendLayout()
        Me.GroupBox3.SuspendLayout()
        Me.SuspendLayout()
        '
        'txtMaxProfitPerDay
        '
        Me.txtMaxProfitPerDay.Location = New System.Drawing.Point(174, 301)
        Me.txtMaxProfitPerDay.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxProfitPerDay.Name = "txtMaxProfitPerDay"
        Me.txtMaxProfitPerDay.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxProfitPerDay.TabIndex = 9
        '
        'lblMaxProfitPerDay
        '
        Me.lblMaxProfitPerDay.AutoSize = True
        Me.lblMaxProfitPerDay.Location = New System.Drawing.Point(8, 305)
        Me.lblMaxProfitPerDay.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxProfitPerDay.Name = "lblMaxProfitPerDay"
        Me.lblMaxProfitPerDay.Size = New System.Drawing.Size(125, 17)
        Me.lblMaxProfitPerDay.TabIndex = 27
        Me.lblMaxProfitPerDay.Text = "Max Profit Per Day"
        '
        'txtMaxLossPerDay
        '
        Me.txtMaxLossPerDay.Location = New System.Drawing.Point(174, 266)
        Me.txtMaxLossPerDay.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxLossPerDay.Name = "txtMaxLossPerDay"
        Me.txtMaxLossPerDay.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxLossPerDay.TabIndex = 8
        '
        'lblMaxLossPerDay
        '
        Me.lblMaxLossPerDay.AutoSize = True
        Me.lblMaxLossPerDay.Location = New System.Drawing.Point(8, 270)
        Me.lblMaxLossPerDay.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxLossPerDay.Name = "lblMaxLossPerDay"
        Me.lblMaxLossPerDay.Size = New System.Drawing.Size(122, 17)
        Me.lblMaxLossPerDay.TabIndex = 25
        Me.lblMaxLossPerDay.Text = "Max Loss Per Day"
        '
        'dtpckrEODExitTime
        '
        Me.dtpckrEODExitTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrEODExitTime.Location = New System.Drawing.Point(175, 158)
        Me.dtpckrEODExitTime.Name = "dtpckrEODExitTime"
        Me.dtpckrEODExitTime.ShowUpDown = True
        Me.dtpckrEODExitTime.Size = New System.Drawing.Size(255, 22)
        Me.dtpckrEODExitTime.TabIndex = 5
        Me.dtpckrEODExitTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'dtpckrLastTradeEntryTime
        '
        Me.dtpckrLastTradeEntryTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrLastTradeEntryTime.Location = New System.Drawing.Point(176, 123)
        Me.dtpckrLastTradeEntryTime.Name = "dtpckrLastTradeEntryTime"
        Me.dtpckrLastTradeEntryTime.ShowUpDown = True
        Me.dtpckrLastTradeEntryTime.Size = New System.Drawing.Size(255, 22)
        Me.dtpckrLastTradeEntryTime.TabIndex = 4
        Me.dtpckrLastTradeEntryTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'dtpckrTradeStartTime
        '
        Me.dtpckrTradeStartTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrTradeStartTime.Location = New System.Drawing.Point(175, 86)
        Me.dtpckrTradeStartTime.Name = "dtpckrTradeStartTime"
        Me.dtpckrTradeStartTime.ShowUpDown = True
        Me.dtpckrTradeStartTime.Size = New System.Drawing.Size(255, 22)
        Me.dtpckrTradeStartTime.TabIndex = 3
        Me.dtpckrTradeStartTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'lblEODExitTime
        '
        Me.lblEODExitTime.AutoSize = True
        Me.lblEODExitTime.Location = New System.Drawing.Point(9, 159)
        Me.lblEODExitTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblEODExitTime.Name = "lblEODExitTime"
        Me.lblEODExitTime.Size = New System.Drawing.Size(99, 17)
        Me.lblEODExitTime.TabIndex = 23
        Me.lblEODExitTime.Text = "EOD Exit Time"
        '
        'lblLastTradeEntryTime
        '
        Me.lblLastTradeEntryTime.AutoSize = True
        Me.lblLastTradeEntryTime.Location = New System.Drawing.Point(9, 124)
        Me.lblLastTradeEntryTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblLastTradeEntryTime.Name = "lblLastTradeEntryTime"
        Me.lblLastTradeEntryTime.Size = New System.Drawing.Size(149, 17)
        Me.lblLastTradeEntryTime.TabIndex = 21
        Me.lblLastTradeEntryTime.Text = "Last Trade Entry Time"
        '
        'lblTradeStartTime
        '
        Me.lblTradeStartTime.AutoSize = True
        Me.lblTradeStartTime.Location = New System.Drawing.Point(9, 88)
        Me.lblTradeStartTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTradeStartTime.Name = "lblTradeStartTime"
        Me.lblTradeStartTime.Size = New System.Drawing.Size(115, 17)
        Me.lblTradeStartTime.TabIndex = 19
        Me.lblTradeStartTime.Text = "Trade Start Time"
        '
        'btnBrowse
        '
        Me.btnBrowse.Font = New System.Drawing.Font("Microsoft Sans Serif", 7.8!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnBrowse.Location = New System.Drawing.Point(390, 333)
        Me.btnBrowse.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.btnBrowse.Name = "btnBrowse"
        Me.btnBrowse.Size = New System.Drawing.Size(40, 23)
        Me.btnBrowse.TabIndex = 10
        Me.btnBrowse.Text = "..."
        Me.btnBrowse.UseVisualStyleBackColor = True
        '
        'txtInstrumentDetalis
        '
        Me.txtInstrumentDetalis.Location = New System.Drawing.Point(174, 334)
        Me.txtInstrumentDetalis.Margin = New System.Windows.Forms.Padding(4)
        Me.txtInstrumentDetalis.Name = "txtInstrumentDetalis"
        Me.txtInstrumentDetalis.ReadOnly = True
        Me.txtInstrumentDetalis.Size = New System.Drawing.Size(215, 22)
        Me.txtInstrumentDetalis.TabIndex = 11
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.txtMaxCapital)
        Me.GroupBox1.Controls.Add(Me.lblMaxCapital)
        Me.GroupBox1.Controls.Add(Me.txtMaxProfitPerStockMultiplier)
        Me.GroupBox1.Controls.Add(Me.lblMaxProfitPerStockMultiplier)
        Me.GroupBox1.Controls.Add(Me.txtMaxLossPerStockMultiplier)
        Me.GroupBox1.Controls.Add(Me.lblMaxLossPerStockMultiplier)
        Me.GroupBox1.Controls.Add(Me.txtPinbarTailPercentage)
        Me.GroupBox1.Controls.Add(Me.lblPinbarTailPercentage)
        Me.GroupBox1.Controls.Add(Me.txtMinLossPercentagePerTrade)
        Me.GroupBox1.Controls.Add(Me.lblMinLossPercentagePerTrade)
        Me.GroupBox1.Controls.Add(Me.txtMaxLossPerTradeMultiplier)
        Me.GroupBox1.Controls.Add(Me.lblMaxLossPerTrade)
        Me.GroupBox1.Controls.Add(Me.txtTargetMultiplier)
        Me.GroupBox1.Controls.Add(Me.lblTargetMultiplier)
        Me.GroupBox1.Controls.Add(Me.txtNumberOfTradePerStock)
        Me.GroupBox1.Controls.Add(Me.lblNumberOfTradePerStock)
        Me.GroupBox1.Controls.Add(Me.txtATRPeriod)
        Me.GroupBox1.Controls.Add(Me.lblATRPeriod)
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
        Me.GroupBox1.Location = New System.Drawing.Point(5, 6)
        Me.GroupBox1.Margin = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Padding = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Size = New System.Drawing.Size(437, 569)
        Me.GroupBox1.TabIndex = 15
        Me.GroupBox1.TabStop = False
        '
        'txtMaxProfitPerStockMultiplier
        '
        Me.txtMaxProfitPerStockMultiplier.Location = New System.Drawing.Point(174, 439)
        Me.txtMaxProfitPerStockMultiplier.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxProfitPerStockMultiplier.Name = "txtMaxProfitPerStockMultiplier"
        Me.txtMaxProfitPerStockMultiplier.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxProfitPerStockMultiplier.TabIndex = 14
        Me.txtMaxProfitPerStockMultiplier.Tag = ""
        '
        'lblMaxProfitPerStockMultiplier
        '
        Me.lblMaxProfitPerStockMultiplier.AutoSize = True
        Me.lblMaxProfitPerStockMultiplier.Location = New System.Drawing.Point(8, 442)
        Me.lblMaxProfitPerStockMultiplier.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxProfitPerStockMultiplier.Name = "lblMaxProfitPerStockMultiplier"
        Me.lblMaxProfitPerStockMultiplier.Size = New System.Drawing.Size(161, 17)
        Me.lblMaxProfitPerStockMultiplier.TabIndex = 47
        Me.lblMaxProfitPerStockMultiplier.Text = "Max Profit Per Stock Mul"
        '
        'txtMaxLossPerStockMultiplier
        '
        Me.txtMaxLossPerStockMultiplier.Location = New System.Drawing.Point(174, 405)
        Me.txtMaxLossPerStockMultiplier.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxLossPerStockMultiplier.Name = "txtMaxLossPerStockMultiplier"
        Me.txtMaxLossPerStockMultiplier.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxLossPerStockMultiplier.TabIndex = 13
        Me.txtMaxLossPerStockMultiplier.Tag = ""
        '
        'lblMaxLossPerStockMultiplier
        '
        Me.lblMaxLossPerStockMultiplier.AutoSize = True
        Me.lblMaxLossPerStockMultiplier.Location = New System.Drawing.Point(8, 408)
        Me.lblMaxLossPerStockMultiplier.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxLossPerStockMultiplier.Name = "lblMaxLossPerStockMultiplier"
        Me.lblMaxLossPerStockMultiplier.Size = New System.Drawing.Size(158, 17)
        Me.lblMaxLossPerStockMultiplier.TabIndex = 45
        Me.lblMaxLossPerStockMultiplier.Text = "Max Loss Per Stock Mul"
        '
        'txtPinbarTailPercentage
        '
        Me.txtPinbarTailPercentage.Location = New System.Drawing.Point(174, 370)
        Me.txtPinbarTailPercentage.Margin = New System.Windows.Forms.Padding(4)
        Me.txtPinbarTailPercentage.Name = "txtPinbarTailPercentage"
        Me.txtPinbarTailPercentage.Size = New System.Drawing.Size(255, 22)
        Me.txtPinbarTailPercentage.TabIndex = 12
        Me.txtPinbarTailPercentage.Tag = "Max Loss Per Day"
        '
        'lblPinbarTailPercentage
        '
        Me.lblPinbarTailPercentage.AutoSize = True
        Me.lblPinbarTailPercentage.Location = New System.Drawing.Point(9, 373)
        Me.lblPinbarTailPercentage.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblPinbarTailPercentage.Name = "lblPinbarTailPercentage"
        Me.lblPinbarTailPercentage.Size = New System.Drawing.Size(92, 17)
        Me.lblPinbarTailPercentage.TabIndex = 44
        Me.lblPinbarTailPercentage.Text = "Pinbar Tail %"
        '
        'txtMinLossPercentagePerTrade
        '
        Me.txtMinLossPercentagePerTrade.Location = New System.Drawing.Point(174, 505)
        Me.txtMinLossPercentagePerTrade.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMinLossPercentagePerTrade.Name = "txtMinLossPercentagePerTrade"
        Me.txtMinLossPercentagePerTrade.Size = New System.Drawing.Size(255, 22)
        Me.txtMinLossPercentagePerTrade.TabIndex = 16
        '
        'lblMinLossPercentagePerTrade
        '
        Me.lblMinLossPercentagePerTrade.AutoSize = True
        Me.lblMinLossPercentagePerTrade.Location = New System.Drawing.Point(8, 509)
        Me.lblMinLossPercentagePerTrade.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMinLossPercentagePerTrade.Name = "lblMinLossPercentagePerTrade"
        Me.lblMinLossPercentagePerTrade.Size = New System.Drawing.Size(148, 17)
        Me.lblMinLossPercentagePerTrade.TabIndex = 41
        Me.lblMinLossPercentagePerTrade.Text = "Min Loss % Per Trade"
        '
        'txtMaxLossPerTradeMultiplier
        '
        Me.txtMaxLossPerTradeMultiplier.Location = New System.Drawing.Point(174, 470)
        Me.txtMaxLossPerTradeMultiplier.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxLossPerTradeMultiplier.Name = "txtMaxLossPerTradeMultiplier"
        Me.txtMaxLossPerTradeMultiplier.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxLossPerTradeMultiplier.TabIndex = 15
        '
        'lblMaxLossPerTrade
        '
        Me.lblMaxLossPerTrade.AutoSize = True
        Me.lblMaxLossPerTrade.Location = New System.Drawing.Point(8, 474)
        Me.lblMaxLossPerTrade.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxLossPerTrade.Name = "lblMaxLossPerTrade"
        Me.lblMaxLossPerTrade.Size = New System.Drawing.Size(161, 17)
        Me.lblMaxLossPerTrade.TabIndex = 40
        Me.lblMaxLossPerTrade.Text = "Max Loss Per Trade Mul"
        '
        'txtTargetMultiplier
        '
        Me.txtTargetMultiplier.Location = New System.Drawing.Point(174, 230)
        Me.txtTargetMultiplier.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTargetMultiplier.Name = "txtTargetMultiplier"
        Me.txtTargetMultiplier.Size = New System.Drawing.Size(255, 22)
        Me.txtTargetMultiplier.TabIndex = 7
        Me.txtTargetMultiplier.Tag = "Max Profit Per Day"
        '
        'lblTargetMultiplier
        '
        Me.lblTargetMultiplier.AutoSize = True
        Me.lblTargetMultiplier.Location = New System.Drawing.Point(8, 233)
        Me.lblTargetMultiplier.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTargetMultiplier.Name = "lblTargetMultiplier"
        Me.lblTargetMultiplier.Size = New System.Drawing.Size(110, 17)
        Me.lblTargetMultiplier.TabIndex = 37
        Me.lblTargetMultiplier.Text = "Target Multiplier"
        '
        'txtNumberOfTradePerStock
        '
        Me.txtNumberOfTradePerStock.Location = New System.Drawing.Point(174, 195)
        Me.txtNumberOfTradePerStock.Margin = New System.Windows.Forms.Padding(4)
        Me.txtNumberOfTradePerStock.Name = "txtNumberOfTradePerStock"
        Me.txtNumberOfTradePerStock.Size = New System.Drawing.Size(255, 22)
        Me.txtNumberOfTradePerStock.TabIndex = 6
        Me.txtNumberOfTradePerStock.Tag = "Max Loss Per Day"
        '
        'lblNumberOfTradePerStock
        '
        Me.lblNumberOfTradePerStock.AutoSize = True
        Me.lblNumberOfTradePerStock.Location = New System.Drawing.Point(9, 198)
        Me.lblNumberOfTradePerStock.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblNumberOfTradePerStock.Name = "lblNumberOfTradePerStock"
        Me.lblNumberOfTradePerStock.Size = New System.Drawing.Size(152, 17)
        Me.lblNumberOfTradePerStock.TabIndex = 36
        Me.lblNumberOfTradePerStock.Text = "No Of Trade Per Stock"
        '
        'txtATRPeriod
        '
        Me.txtATRPeriod.Location = New System.Drawing.Point(176, 14)
        Me.txtATRPeriod.Margin = New System.Windows.Forms.Padding(4)
        Me.txtATRPeriod.Name = "txtATRPeriod"
        Me.txtATRPeriod.Size = New System.Drawing.Size(253, 22)
        Me.txtATRPeriod.TabIndex = 1
        '
        'lblATRPeriod
        '
        Me.lblATRPeriod.AutoSize = True
        Me.lblATRPeriod.Location = New System.Drawing.Point(9, 19)
        Me.lblATRPeriod.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblATRPeriod.Name = "lblATRPeriod"
        Me.lblATRPeriod.Size = New System.Drawing.Size(81, 17)
        Me.lblATRPeriod.TabIndex = 33
        Me.lblATRPeriod.Text = "ATR Period"
        '
        'lblInstrumentDetails
        '
        Me.lblInstrumentDetails.AutoSize = True
        Me.lblInstrumentDetails.Location = New System.Drawing.Point(8, 337)
        Me.lblInstrumentDetails.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblInstrumentDetails.Name = "lblInstrumentDetails"
        Me.lblInstrumentDetails.Size = New System.Drawing.Size(121, 17)
        Me.lblInstrumentDetails.TabIndex = 8
        Me.lblInstrumentDetails.Text = "Instrument Details"
        '
        'txtSignalTimeFrame
        '
        Me.txtSignalTimeFrame.Location = New System.Drawing.Point(175, 49)
        Me.txtSignalTimeFrame.Margin = New System.Windows.Forms.Padding(4)
        Me.txtSignalTimeFrame.Name = "txtSignalTimeFrame"
        Me.txtSignalTimeFrame.Size = New System.Drawing.Size(255, 22)
        Me.txtSignalTimeFrame.TabIndex = 2
        '
        'lblSignalTimeFrame
        '
        Me.lblSignalTimeFrame.AutoSize = True
        Me.lblSignalTimeFrame.Location = New System.Drawing.Point(9, 52)
        Me.lblSignalTimeFrame.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblSignalTimeFrame.Name = "lblSignalTimeFrame"
        Me.lblSignalTimeFrame.Size = New System.Drawing.Size(158, 17)
        Me.lblSignalTimeFrame.TabIndex = 3
        Me.lblSignalTimeFrame.Text = "Signal Time Frame(min)"
        '
        'ImageList1
        '
        Me.ImageList1.ImageStream = CType(resources.GetObject("ImageList1.ImageStream"), System.Windows.Forms.ImageListStreamer)
        Me.ImageList1.TransparentColor = System.Drawing.Color.Transparent
        Me.ImageList1.Images.SetKeyName(0, "save-icon-36533.png")
        '
        'btnSavePetDGandhiSettings
        '
        Me.btnSavePetDGandhiSettings.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnSavePetDGandhiSettings.ImageKey = "save-icon-36533.png"
        Me.btnSavePetDGandhiSettings.ImageList = Me.ImageList1
        Me.btnSavePetDGandhiSettings.Location = New System.Drawing.Point(829, 13)
        Me.btnSavePetDGandhiSettings.Margin = New System.Windows.Forms.Padding(4)
        Me.btnSavePetDGandhiSettings.Name = "btnSavePetDGandhiSettings"
        Me.btnSavePetDGandhiSettings.Size = New System.Drawing.Size(112, 58)
        Me.btnSavePetDGandhiSettings.TabIndex = 0
        Me.btnSavePetDGandhiSettings.Text = "&Save"
        Me.btnSavePetDGandhiSettings.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnSavePetDGandhiSettings.UseVisualStyleBackColor = True
        '
        'opnFileSettings
        '
        '
        'grpTelegram
        '
        Me.grpTelegram.Controls.Add(Me.txtTelegramSignalChatID)
        Me.grpTelegram.Controls.Add(Me.lblTelegramSignalChatID)
        Me.grpTelegram.Controls.Add(Me.txtTelegramTargetChatID)
        Me.grpTelegram.Controls.Add(Me.lblTelegramTargetChatID)
        Me.grpTelegram.Controls.Add(Me.txtTelegramMTMChatID)
        Me.grpTelegram.Controls.Add(Me.lblMTMChatID)
        Me.grpTelegram.Controls.Add(Me.txtTelegramTradeChatID)
        Me.grpTelegram.Controls.Add(Me.lblTelegramTradeChatID)
        Me.grpTelegram.Controls.Add(Me.txtTelegramAPI)
        Me.grpTelegram.Controls.Add(Me.lblTelegramAPI)
        Me.grpTelegram.Location = New System.Drawing.Point(452, 169)
        Me.grpTelegram.Name = "grpTelegram"
        Me.grpTelegram.Size = New System.Drawing.Size(358, 189)
        Me.grpTelegram.TabIndex = 19
        Me.grpTelegram.TabStop = False
        Me.grpTelegram.Text = "Telegram Details"
        '
        'txtTelegramSignalChatID
        '
        Me.txtTelegramSignalChatID.Location = New System.Drawing.Point(146, 88)
        Me.txtTelegramSignalChatID.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTelegramSignalChatID.Name = "txtTelegramSignalChatID"
        Me.txtTelegramSignalChatID.Size = New System.Drawing.Size(201, 22)
        Me.txtTelegramSignalChatID.TabIndex = 24
        '
        'lblTelegramSignalChatID
        '
        Me.lblTelegramSignalChatID.AutoSize = True
        Me.lblTelegramSignalChatID.Location = New System.Drawing.Point(9, 92)
        Me.lblTelegramSignalChatID.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTelegramSignalChatID.Name = "lblTelegramSignalChatID"
        Me.lblTelegramSignalChatID.Size = New System.Drawing.Size(97, 17)
        Me.lblTelegramSignalChatID.TabIndex = 41
        Me.lblTelegramSignalChatID.Text = "Signal Chat ID"
        '
        'txtTelegramTargetChatID
        '
        Me.txtTelegramTargetChatID.Location = New System.Drawing.Point(146, 119)
        Me.txtTelegramTargetChatID.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTelegramTargetChatID.Name = "txtTelegramTargetChatID"
        Me.txtTelegramTargetChatID.Size = New System.Drawing.Size(201, 22)
        Me.txtTelegramTargetChatID.TabIndex = 25
        '
        'lblTelegramTargetChatID
        '
        Me.lblTelegramTargetChatID.AutoSize = True
        Me.lblTelegramTargetChatID.Location = New System.Drawing.Point(9, 123)
        Me.lblTelegramTargetChatID.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTelegramTargetChatID.Name = "lblTelegramTargetChatID"
        Me.lblTelegramTargetChatID.Size = New System.Drawing.Size(100, 17)
        Me.lblTelegramTargetChatID.TabIndex = 39
        Me.lblTelegramTargetChatID.Text = "Target Chat ID"
        '
        'txtTelegramMTMChatID
        '
        Me.txtTelegramMTMChatID.Location = New System.Drawing.Point(146, 150)
        Me.txtTelegramMTMChatID.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTelegramMTMChatID.Name = "txtTelegramMTMChatID"
        Me.txtTelegramMTMChatID.Size = New System.Drawing.Size(201, 22)
        Me.txtTelegramMTMChatID.TabIndex = 26
        '
        'lblMTMChatID
        '
        Me.lblMTMChatID.AutoSize = True
        Me.lblMTMChatID.Location = New System.Drawing.Point(9, 154)
        Me.lblMTMChatID.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMTMChatID.Name = "lblMTMChatID"
        Me.lblMTMChatID.Size = New System.Drawing.Size(89, 17)
        Me.lblMTMChatID.TabIndex = 37
        Me.lblMTMChatID.Text = "MTM Chat ID"
        '
        'txtTelegramTradeChatID
        '
        Me.txtTelegramTradeChatID.Location = New System.Drawing.Point(146, 56)
        Me.txtTelegramTradeChatID.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTelegramTradeChatID.Name = "txtTelegramTradeChatID"
        Me.txtTelegramTradeChatID.Size = New System.Drawing.Size(201, 22)
        Me.txtTelegramTradeChatID.TabIndex = 23
        '
        'lblTelegramTradeChatID
        '
        Me.lblTelegramTradeChatID.AutoSize = True
        Me.lblTelegramTradeChatID.Location = New System.Drawing.Point(9, 60)
        Me.lblTelegramTradeChatID.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTelegramTradeChatID.Name = "lblTelegramTradeChatID"
        Me.lblTelegramTradeChatID.Size = New System.Drawing.Size(96, 17)
        Me.lblTelegramTradeChatID.TabIndex = 35
        Me.lblTelegramTradeChatID.Text = "Trade Chat ID"
        '
        'txtTelegramAPI
        '
        Me.txtTelegramAPI.Location = New System.Drawing.Point(146, 24)
        Me.txtTelegramAPI.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTelegramAPI.Name = "txtTelegramAPI"
        Me.txtTelegramAPI.Size = New System.Drawing.Size(201, 22)
        Me.txtTelegramAPI.TabIndex = 22
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
        'GroupBox4
        '
        Me.GroupBox4.Controls.Add(Me.chbAllowToIncreaseQuantity)
        Me.GroupBox4.Controls.Add(Me.chbAutoSelectStock)
        Me.GroupBox4.Controls.Add(Me.txtMinCapitalPerStock)
        Me.GroupBox4.Controls.Add(Me.lblMinCapitalPerStock)
        Me.GroupBox4.Controls.Add(Me.chbFuture)
        Me.GroupBox4.Controls.Add(Me.chbCash)
        Me.GroupBox4.Location = New System.Drawing.Point(452, 5)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(358, 141)
        Me.GroupBox4.TabIndex = 38
        Me.GroupBox4.TabStop = False
        '
        'chbAllowToIncreaseQuantity
        '
        Me.chbAllowToIncreaseQuantity.AutoSize = True
        Me.chbAllowToIncreaseQuantity.Location = New System.Drawing.Point(12, 72)
        Me.chbAllowToIncreaseQuantity.Name = "chbAllowToIncreaseQuantity"
        Me.chbAllowToIncreaseQuantity.Size = New System.Drawing.Size(198, 21)
        Me.chbAllowToIncreaseQuantity.TabIndex = 20
        Me.chbAllowToIncreaseQuantity.Text = "Allow To Increase Quantity"
        Me.chbAllowToIncreaseQuantity.UseVisualStyleBackColor = True
        '
        'chbAutoSelectStock
        '
        Me.chbAutoSelectStock.AutoSize = True
        Me.chbAutoSelectStock.Location = New System.Drawing.Point(12, 14)
        Me.chbAutoSelectStock.Name = "chbAutoSelectStock"
        Me.chbAutoSelectStock.Size = New System.Drawing.Size(141, 21)
        Me.chbAutoSelectStock.TabIndex = 17
        Me.chbAutoSelectStock.Text = "Auto Select Stock"
        Me.chbAutoSelectStock.UseVisualStyleBackColor = True
        '
        'txtMinCapitalPerStock
        '
        Me.txtMinCapitalPerStock.Location = New System.Drawing.Point(146, 101)
        Me.txtMinCapitalPerStock.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMinCapitalPerStock.Name = "txtMinCapitalPerStock"
        Me.txtMinCapitalPerStock.Size = New System.Drawing.Size(201, 22)
        Me.txtMinCapitalPerStock.TabIndex = 21
        Me.txtMinCapitalPerStock.Tag = ""
        '
        'lblMinCapitalPerStock
        '
        Me.lblMinCapitalPerStock.AutoSize = True
        Me.lblMinCapitalPerStock.Location = New System.Drawing.Point(6, 104)
        Me.lblMinCapitalPerStock.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMinCapitalPerStock.Name = "lblMinCapitalPerStock"
        Me.lblMinCapitalPerStock.Size = New System.Drawing.Size(142, 17)
        Me.lblMinCapitalPerStock.TabIndex = 37
        Me.lblMinCapitalPerStock.Text = "Min Capital Per Stock"
        '
        'chbFuture
        '
        Me.chbFuture.AutoSize = True
        Me.chbFuture.Location = New System.Drawing.Point(84, 43)
        Me.chbFuture.Name = "chbFuture"
        Me.chbFuture.Size = New System.Drawing.Size(71, 21)
        Me.chbFuture.TabIndex = 19
        Me.chbFuture.Text = "Future"
        Me.chbFuture.UseVisualStyleBackColor = True
        '
        'chbCash
        '
        Me.chbCash.AutoSize = True
        Me.chbCash.Location = New System.Drawing.Point(12, 42)
        Me.chbCash.Name = "chbCash"
        Me.chbCash.Size = New System.Drawing.Size(62, 21)
        Me.chbCash.TabIndex = 18
        Me.chbCash.Text = "Cash"
        Me.chbCash.UseVisualStyleBackColor = True
        '
        'GroupBox3
        '
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
        Me.GroupBox3.Location = New System.Drawing.Point(452, 371)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(358, 204)
        Me.GroupBox3.TabIndex = 39
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "Stock Selection Settings"
        '
        'txtBlankCandlePercentage
        '
        Me.txtBlankCandlePercentage.Location = New System.Drawing.Point(146, 143)
        Me.txtBlankCandlePercentage.Margin = New System.Windows.Forms.Padding(4)
        Me.txtBlankCandlePercentage.Name = "txtBlankCandlePercentage"
        Me.txtBlankCandlePercentage.Size = New System.Drawing.Size(201, 22)
        Me.txtBlankCandlePercentage.TabIndex = 31
        Me.txtBlankCandlePercentage.Tag = "Number Of Stock"
        '
        'lblBlankCandlePercentage
        '
        Me.lblBlankCandlePercentage.AutoSize = True
        Me.lblBlankCandlePercentage.Location = New System.Drawing.Point(10, 146)
        Me.lblBlankCandlePercentage.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblBlankCandlePercentage.Name = "lblBlankCandlePercentage"
        Me.lblBlankCandlePercentage.Size = New System.Drawing.Size(107, 17)
        Me.lblBlankCandlePercentage.TabIndex = 45
        Me.lblBlankCandlePercentage.Text = "Blank Candle %"
        '
        'txtNumberOfStock
        '
        Me.txtNumberOfStock.Location = New System.Drawing.Point(146, 173)
        Me.txtNumberOfStock.Margin = New System.Windows.Forms.Padding(4)
        Me.txtNumberOfStock.Name = "txtNumberOfStock"
        Me.txtNumberOfStock.Size = New System.Drawing.Size(201, 22)
        Me.txtNumberOfStock.TabIndex = 32
        Me.txtNumberOfStock.Tag = "Number Of Stock"
        '
        'lblNumberOfStock
        '
        Me.lblNumberOfStock.AutoSize = True
        Me.lblNumberOfStock.Location = New System.Drawing.Point(9, 176)
        Me.lblNumberOfStock.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblNumberOfStock.Name = "lblNumberOfStock"
        Me.lblNumberOfStock.Size = New System.Drawing.Size(116, 17)
        Me.lblNumberOfStock.TabIndex = 43
        Me.lblNumberOfStock.Text = "Number Of Stock"
        '
        'txtMinVolume
        '
        Me.txtMinVolume.Location = New System.Drawing.Point(146, 114)
        Me.txtMinVolume.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMinVolume.Name = "txtMinVolume"
        Me.txtMinVolume.Size = New System.Drawing.Size(201, 22)
        Me.txtMinVolume.TabIndex = 30
        Me.txtMinVolume.Tag = "Min Volume"
        '
        'lblMinVolume
        '
        Me.lblMinVolume.AutoSize = True
        Me.lblMinVolume.Location = New System.Drawing.Point(9, 114)
        Me.lblMinVolume.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMinVolume.Name = "lblMinVolume"
        Me.lblMinVolume.Size = New System.Drawing.Size(81, 17)
        Me.lblMinVolume.TabIndex = 41
        Me.lblMinVolume.Text = "Min Volume"
        '
        'txtATRPercentage
        '
        Me.txtATRPercentage.Location = New System.Drawing.Point(146, 84)
        Me.txtATRPercentage.Margin = New System.Windows.Forms.Padding(4)
        Me.txtATRPercentage.Name = "txtATRPercentage"
        Me.txtATRPercentage.Size = New System.Drawing.Size(201, 22)
        Me.txtATRPercentage.TabIndex = 29
        Me.txtATRPercentage.Tag = "ATR %"
        '
        'lblATR
        '
        Me.lblATR.AutoSize = True
        Me.lblATR.Location = New System.Drawing.Point(9, 87)
        Me.lblATR.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblATR.Name = "lblATR"
        Me.lblATR.Size = New System.Drawing.Size(52, 17)
        Me.lblATR.TabIndex = 39
        Me.lblATR.Text = "ATR %"
        '
        'txtMaxPrice
        '
        Me.txtMaxPrice.Location = New System.Drawing.Point(146, 55)
        Me.txtMaxPrice.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxPrice.Name = "txtMaxPrice"
        Me.txtMaxPrice.Size = New System.Drawing.Size(201, 22)
        Me.txtMaxPrice.TabIndex = 28
        Me.txtMaxPrice.Tag = "Max Price"
        '
        'lblMaxPrice
        '
        Me.lblMaxPrice.AutoSize = True
        Me.lblMaxPrice.Location = New System.Drawing.Point(9, 59)
        Me.lblMaxPrice.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxPrice.Name = "lblMaxPrice"
        Me.lblMaxPrice.Size = New System.Drawing.Size(69, 17)
        Me.lblMaxPrice.TabIndex = 37
        Me.lblMaxPrice.Text = "Max Price"
        '
        'txtMinPrice
        '
        Me.txtMinPrice.Location = New System.Drawing.Point(146, 25)
        Me.txtMinPrice.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMinPrice.Name = "txtMinPrice"
        Me.txtMinPrice.Size = New System.Drawing.Size(201, 22)
        Me.txtMinPrice.TabIndex = 27
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
        'txtMaxCapital
        '
        Me.txtMaxCapital.Location = New System.Drawing.Point(174, 539)
        Me.txtMaxCapital.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxCapital.Name = "txtMaxCapital"
        Me.txtMaxCapital.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxCapital.TabIndex = 48
        '
        'lblMaxCapital
        '
        Me.lblMaxCapital.AutoSize = True
        Me.lblMaxCapital.Location = New System.Drawing.Point(8, 543)
        Me.lblMaxCapital.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxCapital.Name = "lblMaxCapital"
        Me.lblMaxCapital.Size = New System.Drawing.Size(159, 17)
        Me.lblMaxCapital.TabIndex = 49
        Me.lblMaxCapital.Text = "Max Capital To Be Used"
        '
        'frmPetDGandhiSettings
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(947, 580)
        Me.Controls.Add(Me.GroupBox3)
        Me.Controls.Add(Me.GroupBox4)
        Me.Controls.Add(Me.grpTelegram)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.btnSavePetDGandhiSettings)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "frmPetDGandhiSettings"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Pet-D Gandhi Strategy - Settings"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.grpTelegram.ResumeLayout(False)
        Me.grpTelegram.PerformLayout()
        Me.GroupBox4.ResumeLayout(False)
        Me.GroupBox4.PerformLayout()
        Me.GroupBox3.ResumeLayout(False)
        Me.GroupBox3.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
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
    Friend WithEvents ImageList1 As ImageList
    Friend WithEvents btnSavePetDGandhiSettings As Button
    Friend WithEvents opnFileSettings As OpenFileDialog
    Friend WithEvents grpTelegram As GroupBox
    Friend WithEvents txtTelegramMTMChatID As TextBox
    Friend WithEvents lblMTMChatID As Label
    Friend WithEvents txtTelegramTradeChatID As TextBox
    Friend WithEvents lblTelegramTradeChatID As Label
    Friend WithEvents txtTelegramAPI As TextBox
    Friend WithEvents lblTelegramAPI As Label
    Friend WithEvents txtATRPeriod As TextBox
    Friend WithEvents lblATRPeriod As Label
    Friend WithEvents txtTargetMultiplier As TextBox
    Friend WithEvents lblTargetMultiplier As Label
    Friend WithEvents txtNumberOfTradePerStock As TextBox
    Friend WithEvents lblNumberOfTradePerStock As Label
    Friend WithEvents GroupBox4 As GroupBox
    Friend WithEvents chbAllowToIncreaseQuantity As CheckBox
    Friend WithEvents chbAutoSelectStock As CheckBox
    Friend WithEvents txtMinCapitalPerStock As TextBox
    Friend WithEvents lblMinCapitalPerStock As Label
    Friend WithEvents chbFuture As CheckBox
    Friend WithEvents chbCash As CheckBox
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
    Friend WithEvents txtBlankCandlePercentage As TextBox
    Friend WithEvents lblBlankCandlePercentage As Label
    Friend WithEvents txtTelegramTargetChatID As TextBox
    Friend WithEvents lblTelegramTargetChatID As Label
    Friend WithEvents txtMaxLossPerStockMultiplier As TextBox
    Friend WithEvents lblMaxLossPerStockMultiplier As Label
    Friend WithEvents txtPinbarTailPercentage As TextBox
    Friend WithEvents lblPinbarTailPercentage As Label
    Friend WithEvents txtMinLossPercentagePerTrade As TextBox
    Friend WithEvents lblMinLossPercentagePerTrade As Label
    Friend WithEvents txtMaxLossPerTradeMultiplier As TextBox
    Friend WithEvents lblMaxLossPerTrade As Label
    Friend WithEvents txtMaxProfitPerStockMultiplier As TextBox
    Friend WithEvents lblMaxProfitPerStockMultiplier As Label
    Friend WithEvents txtTelegramSignalChatID As TextBox
    Friend WithEvents lblTelegramSignalChatID As Label
    Friend WithEvents txtMaxCapital As TextBox
    Friend WithEvents lblMaxCapital As Label
End Class
