﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
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
        Me.lblMaxProfitPercentagePerDay = New System.Windows.Forms.Label()
        Me.txtMaxLossPerDay = New System.Windows.Forms.TextBox()
        Me.lblMaxLossPercentagePerDay = New System.Windows.Forms.Label()
        Me.dtpckrEODExitTime = New System.Windows.Forms.DateTimePicker()
        Me.dtpckrLastTradeEntryTime = New System.Windows.Forms.DateTimePicker()
        Me.dtpckrTradeStartTime = New System.Windows.Forms.DateTimePicker()
        Me.lblEODExitTime = New System.Windows.Forms.Label()
        Me.lblLastTradeEntryTime = New System.Windows.Forms.Label()
        Me.lblTradeStartTime = New System.Windows.Forms.Label()
        Me.btnBrowse = New System.Windows.Forms.Button()
        Me.txtInstrumentDetalis = New System.Windows.Forms.TextBox()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.txtMaxLossPercentagePerStock = New System.Windows.Forms.TextBox()
        Me.lblMaxLossPercentagePerStock = New System.Windows.Forms.Label()
        Me.txtPinbarTalePercentage = New System.Windows.Forms.TextBox()
        Me.lblPinbarTalePercentage = New System.Windows.Forms.Label()
        Me.txtMinLossPercentagePerTrade = New System.Windows.Forms.TextBox()
        Me.lblMinLossPercentagePerTrade = New System.Windows.Forms.Label()
        Me.txtMaxLossPercentagePerTrade = New System.Windows.Forms.TextBox()
        Me.lblMaxLossPercentagePerTrade = New System.Windows.Forms.Label()
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
        Me.txtTelegramTargetChatID = New System.Windows.Forms.TextBox()
        Me.lblTelegramTargetChatID = New System.Windows.Forms.Label()
        Me.txtTelegramMTMChatID = New System.Windows.Forms.TextBox()
        Me.lblMTMChatID = New System.Windows.Forms.Label()
        Me.txtTelegramTradeChatID = New System.Windows.Forms.TextBox()
        Me.lblTelegramTradeChatID = New System.Windows.Forms.Label()
        Me.txtTelegramAPI = New System.Windows.Forms.TextBox()
        Me.lblTelegramAPI = New System.Windows.Forms.Label()
        Me.GroupBox4 = New System.Windows.Forms.GroupBox()
        Me.chbAllowToIncreaseCapital = New System.Windows.Forms.CheckBox()
        Me.chbAutoSelectStock = New System.Windows.Forms.CheckBox()
        Me.txtMinCapital = New System.Windows.Forms.TextBox()
        Me.lblFutureMinCapital = New System.Windows.Forms.Label()
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
        Me.txtMaxProfitPerDay.TabIndex = 6
        '
        'lblMaxProfitPercentagePerDay
        '
        Me.lblMaxProfitPercentagePerDay.AutoSize = True
        Me.lblMaxProfitPercentagePerDay.Location = New System.Drawing.Point(8, 305)
        Me.lblMaxProfitPercentagePerDay.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxProfitPercentagePerDay.Name = "lblMaxProfitPercentagePerDay"
        Me.lblMaxProfitPercentagePerDay.Size = New System.Drawing.Size(125, 17)
        Me.lblMaxProfitPercentagePerDay.TabIndex = 27
        Me.lblMaxProfitPercentagePerDay.Text = "Max Profit Per Day"
        '
        'txtMaxLossPerDay
        '
        Me.txtMaxLossPerDay.Location = New System.Drawing.Point(174, 266)
        Me.txtMaxLossPerDay.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxLossPerDay.Name = "txtMaxLossPerDay"
        Me.txtMaxLossPerDay.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxLossPerDay.TabIndex = 5
        '
        'lblMaxLossPercentagePerDay
        '
        Me.lblMaxLossPercentagePerDay.AutoSize = True
        Me.lblMaxLossPercentagePerDay.Location = New System.Drawing.Point(8, 270)
        Me.lblMaxLossPercentagePerDay.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxLossPercentagePerDay.Name = "lblMaxLossPercentagePerDay"
        Me.lblMaxLossPercentagePerDay.Size = New System.Drawing.Size(122, 17)
        Me.lblMaxLossPercentagePerDay.TabIndex = 25
        Me.lblMaxLossPercentagePerDay.Text = "Max Loss Per Day"
        '
        'dtpckrEODExitTime
        '
        Me.dtpckrEODExitTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrEODExitTime.Location = New System.Drawing.Point(175, 158)
        Me.dtpckrEODExitTime.Name = "dtpckrEODExitTime"
        Me.dtpckrEODExitTime.ShowUpDown = True
        Me.dtpckrEODExitTime.Size = New System.Drawing.Size(255, 22)
        Me.dtpckrEODExitTime.TabIndex = 3
        Me.dtpckrEODExitTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'dtpckrLastTradeEntryTime
        '
        Me.dtpckrLastTradeEntryTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrLastTradeEntryTime.Location = New System.Drawing.Point(176, 123)
        Me.dtpckrLastTradeEntryTime.Name = "dtpckrLastTradeEntryTime"
        Me.dtpckrLastTradeEntryTime.ShowUpDown = True
        Me.dtpckrLastTradeEntryTime.Size = New System.Drawing.Size(255, 22)
        Me.dtpckrLastTradeEntryTime.TabIndex = 2
        Me.dtpckrLastTradeEntryTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'dtpckrTradeStartTime
        '
        Me.dtpckrTradeStartTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrTradeStartTime.Location = New System.Drawing.Point(175, 86)
        Me.dtpckrTradeStartTime.Name = "dtpckrTradeStartTime"
        Me.dtpckrTradeStartTime.ShowUpDown = True
        Me.dtpckrTradeStartTime.Size = New System.Drawing.Size(255, 22)
        Me.dtpckrTradeStartTime.TabIndex = 1
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
        Me.btnBrowse.Location = New System.Drawing.Point(404, 333)
        Me.btnBrowse.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.btnBrowse.Name = "btnBrowse"
        Me.btnBrowse.Size = New System.Drawing.Size(40, 23)
        Me.btnBrowse.TabIndex = 8
        Me.btnBrowse.Text = "..."
        Me.btnBrowse.UseVisualStyleBackColor = True
        '
        'txtInstrumentDetalis
        '
        Me.txtInstrumentDetalis.Location = New System.Drawing.Point(174, 334)
        Me.txtInstrumentDetalis.Margin = New System.Windows.Forms.Padding(4)
        Me.txtInstrumentDetalis.Name = "txtInstrumentDetalis"
        Me.txtInstrumentDetalis.ReadOnly = True
        Me.txtInstrumentDetalis.Size = New System.Drawing.Size(223, 22)
        Me.txtInstrumentDetalis.TabIndex = 15
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.txtMaxLossPercentagePerStock)
        Me.GroupBox1.Controls.Add(Me.lblMaxLossPercentagePerStock)
        Me.GroupBox1.Controls.Add(Me.txtPinbarTalePercentage)
        Me.GroupBox1.Controls.Add(Me.lblPinbarTalePercentage)
        Me.GroupBox1.Controls.Add(Me.txtMinLossPercentagePerTrade)
        Me.GroupBox1.Controls.Add(Me.lblMinLossPercentagePerTrade)
        Me.GroupBox1.Controls.Add(Me.txtMaxLossPercentagePerTrade)
        Me.GroupBox1.Controls.Add(Me.lblMaxLossPercentagePerTrade)
        Me.GroupBox1.Controls.Add(Me.txtTargetMultiplier)
        Me.GroupBox1.Controls.Add(Me.lblTargetMultiplier)
        Me.GroupBox1.Controls.Add(Me.txtNumberOfTradePerStock)
        Me.GroupBox1.Controls.Add(Me.lblNumberOfTradePerStock)
        Me.GroupBox1.Controls.Add(Me.txtATRPeriod)
        Me.GroupBox1.Controls.Add(Me.lblATRPeriod)
        Me.GroupBox1.Controls.Add(Me.txtMaxProfitPerDay)
        Me.GroupBox1.Controls.Add(Me.lblMaxProfitPercentagePerDay)
        Me.GroupBox1.Controls.Add(Me.txtMaxLossPerDay)
        Me.GroupBox1.Controls.Add(Me.lblMaxLossPercentagePerDay)
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
        Me.GroupBox1.Size = New System.Drawing.Size(451, 509)
        Me.GroupBox1.TabIndex = 15
        Me.GroupBox1.TabStop = False
        '
        'txtMaxLossPercentagePerStock
        '
        Me.txtMaxLossPercentagePerStock.Location = New System.Drawing.Point(174, 405)
        Me.txtMaxLossPercentagePerStock.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxLossPercentagePerStock.Name = "txtMaxLossPercentagePerStock"
        Me.txtMaxLossPercentagePerStock.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxLossPercentagePerStock.TabIndex = 43
        Me.txtMaxLossPercentagePerStock.Tag = ""
        '
        'lblMaxLossPercentagePerStock
        '
        Me.lblMaxLossPercentagePerStock.AutoSize = True
        Me.lblMaxLossPercentagePerStock.Location = New System.Drawing.Point(8, 408)
        Me.lblMaxLossPercentagePerStock.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxLossPercentagePerStock.Name = "lblMaxLossPercentagePerStock"
        Me.lblMaxLossPercentagePerStock.Size = New System.Drawing.Size(148, 17)
        Me.lblMaxLossPercentagePerStock.TabIndex = 45
        Me.lblMaxLossPercentagePerStock.Text = "Max Loss % Per Stock"
        '
        'txtPinbarTalePercentage
        '
        Me.txtPinbarTalePercentage.Location = New System.Drawing.Point(174, 370)
        Me.txtPinbarTalePercentage.Margin = New System.Windows.Forms.Padding(4)
        Me.txtPinbarTalePercentage.Name = "txtPinbarTalePercentage"
        Me.txtPinbarTalePercentage.Size = New System.Drawing.Size(255, 22)
        Me.txtPinbarTalePercentage.TabIndex = 42
        Me.txtPinbarTalePercentage.Tag = "Max Loss Per Day"
        '
        'lblPinbarTalePercentage
        '
        Me.lblPinbarTalePercentage.AutoSize = True
        Me.lblPinbarTalePercentage.Location = New System.Drawing.Point(9, 373)
        Me.lblPinbarTalePercentage.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblPinbarTalePercentage.Name = "lblPinbarTalePercentage"
        Me.lblPinbarTalePercentage.Size = New System.Drawing.Size(97, 17)
        Me.lblPinbarTalePercentage.TabIndex = 44
        Me.lblPinbarTalePercentage.Text = "Pinbar Tale %"
        '
        'txtMinLossPercentagePerTrade
        '
        Me.txtMinLossPercentagePerTrade.Location = New System.Drawing.Point(174, 476)
        Me.txtMinLossPercentagePerTrade.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMinLossPercentagePerTrade.Name = "txtMinLossPercentagePerTrade"
        Me.txtMinLossPercentagePerTrade.Size = New System.Drawing.Size(255, 22)
        Me.txtMinLossPercentagePerTrade.TabIndex = 39
        '
        'lblMinLossPercentagePerTrade
        '
        Me.lblMinLossPercentagePerTrade.AutoSize = True
        Me.lblMinLossPercentagePerTrade.Location = New System.Drawing.Point(8, 480)
        Me.lblMinLossPercentagePerTrade.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMinLossPercentagePerTrade.Name = "lblMinLossPercentagePerTrade"
        Me.lblMinLossPercentagePerTrade.Size = New System.Drawing.Size(148, 17)
        Me.lblMinLossPercentagePerTrade.TabIndex = 41
        Me.lblMinLossPercentagePerTrade.Text = "Min Loss % Per Trade"
        '
        'txtMaxLossPercentagePerTrade
        '
        Me.txtMaxLossPercentagePerTrade.Location = New System.Drawing.Point(174, 441)
        Me.txtMaxLossPercentagePerTrade.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxLossPercentagePerTrade.Name = "txtMaxLossPercentagePerTrade"
        Me.txtMaxLossPercentagePerTrade.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxLossPercentagePerTrade.TabIndex = 38
        '
        'lblMaxLossPercentagePerTrade
        '
        Me.lblMaxLossPercentagePerTrade.AutoSize = True
        Me.lblMaxLossPercentagePerTrade.Location = New System.Drawing.Point(8, 445)
        Me.lblMaxLossPercentagePerTrade.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxLossPercentagePerTrade.Name = "lblMaxLossPercentagePerTrade"
        Me.lblMaxLossPercentagePerTrade.Size = New System.Drawing.Size(151, 17)
        Me.lblMaxLossPercentagePerTrade.TabIndex = 40
        Me.lblMaxLossPercentagePerTrade.Text = "Max Loss % Per Trade"
        '
        'txtTargetMultiplier
        '
        Me.txtTargetMultiplier.Location = New System.Drawing.Point(174, 230)
        Me.txtTargetMultiplier.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTargetMultiplier.Name = "txtTargetMultiplier"
        Me.txtTargetMultiplier.Size = New System.Drawing.Size(255, 22)
        Me.txtTargetMultiplier.TabIndex = 35
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
        Me.txtNumberOfTradePerStock.TabIndex = 34
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
        Me.txtATRPeriod.TabIndex = 32
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
        Me.txtSignalTimeFrame.TabIndex = 0
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
        Me.btnSavePetDGandhiSettings.TabIndex = 14
        Me.btnSavePetDGandhiSettings.Text = "&Save"
        Me.btnSavePetDGandhiSettings.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnSavePetDGandhiSettings.UseVisualStyleBackColor = True
        '
        'opnFileSettings
        '
        '
        'grpTelegram
        '
        Me.grpTelegram.Controls.Add(Me.txtTelegramTargetChatID)
        Me.grpTelegram.Controls.Add(Me.lblTelegramTargetChatID)
        Me.grpTelegram.Controls.Add(Me.txtTelegramMTMChatID)
        Me.grpTelegram.Controls.Add(Me.lblMTMChatID)
        Me.grpTelegram.Controls.Add(Me.txtTelegramTradeChatID)
        Me.grpTelegram.Controls.Add(Me.lblTelegramTradeChatID)
        Me.grpTelegram.Controls.Add(Me.txtTelegramAPI)
        Me.grpTelegram.Controls.Add(Me.lblTelegramAPI)
        Me.grpTelegram.Location = New System.Drawing.Point(463, 150)
        Me.grpTelegram.Name = "grpTelegram"
        Me.grpTelegram.Size = New System.Drawing.Size(358, 154)
        Me.grpTelegram.TabIndex = 19
        Me.grpTelegram.TabStop = False
        Me.grpTelegram.Text = "Telegram Details"
        '
        'txtTelegramTargetChatID
        '
        Me.txtTelegramTargetChatID.Location = New System.Drawing.Point(146, 87)
        Me.txtTelegramTargetChatID.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTelegramTargetChatID.Name = "txtTelegramTargetChatID"
        Me.txtTelegramTargetChatID.Size = New System.Drawing.Size(201, 22)
        Me.txtTelegramTargetChatID.TabIndex = 38
        '
        'lblTelegramTargetChatID
        '
        Me.lblTelegramTargetChatID.AutoSize = True
        Me.lblTelegramTargetChatID.Location = New System.Drawing.Point(9, 91)
        Me.lblTelegramTargetChatID.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTelegramTargetChatID.Name = "lblTelegramTargetChatID"
        Me.lblTelegramTargetChatID.Size = New System.Drawing.Size(100, 17)
        Me.lblTelegramTargetChatID.TabIndex = 39
        Me.lblTelegramTargetChatID.Text = "Target Chat ID"
        '
        'txtTelegramMTMChatID
        '
        Me.txtTelegramMTMChatID.Location = New System.Drawing.Point(146, 118)
        Me.txtTelegramMTMChatID.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTelegramMTMChatID.Name = "txtTelegramMTMChatID"
        Me.txtTelegramMTMChatID.Size = New System.Drawing.Size(201, 22)
        Me.txtTelegramMTMChatID.TabIndex = 36
        '
        'lblMTMChatID
        '
        Me.lblMTMChatID.AutoSize = True
        Me.lblMTMChatID.Location = New System.Drawing.Point(9, 122)
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
        Me.txtTelegramTradeChatID.TabIndex = 32
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
        Me.txtTelegramAPI.TabIndex = 30
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
        Me.GroupBox4.Controls.Add(Me.chbAllowToIncreaseCapital)
        Me.GroupBox4.Controls.Add(Me.chbAutoSelectStock)
        Me.GroupBox4.Controls.Add(Me.txtMinCapital)
        Me.GroupBox4.Controls.Add(Me.lblFutureMinCapital)
        Me.GroupBox4.Controls.Add(Me.chbFuture)
        Me.GroupBox4.Controls.Add(Me.chbCash)
        Me.GroupBox4.Location = New System.Drawing.Point(463, 7)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(358, 141)
        Me.GroupBox4.TabIndex = 38
        Me.GroupBox4.TabStop = False
        '
        'chbAllowToIncreaseCapital
        '
        Me.chbAllowToIncreaseCapital.AutoSize = True
        Me.chbAllowToIncreaseCapital.Location = New System.Drawing.Point(12, 72)
        Me.chbAllowToIncreaseCapital.Name = "chbAllowToIncreaseCapital"
        Me.chbAllowToIncreaseCapital.Size = New System.Drawing.Size(188, 21)
        Me.chbAllowToIncreaseCapital.TabIndex = 41
        Me.chbAllowToIncreaseCapital.Text = "Allow To Increase Capital"
        Me.chbAllowToIncreaseCapital.UseVisualStyleBackColor = True
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
        'txtMinCapital
        '
        Me.txtMinCapital.Location = New System.Drawing.Point(146, 101)
        Me.txtMinCapital.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMinCapital.Name = "txtMinCapital"
        Me.txtMinCapital.Size = New System.Drawing.Size(201, 22)
        Me.txtMinCapital.TabIndex = 36
        Me.txtMinCapital.Tag = ""
        '
        'lblFutureMinCapital
        '
        Me.lblFutureMinCapital.AutoSize = True
        Me.lblFutureMinCapital.Location = New System.Drawing.Point(6, 104)
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
        Me.GroupBox3.Location = New System.Drawing.Point(463, 311)
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
        Me.txtBlankCandlePercentage.TabIndex = 44
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
        Me.txtNumberOfStock.TabIndex = 42
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
        Me.txtMinVolume.TabIndex = 40
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
        Me.txtATRPercentage.TabIndex = 38
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
        Me.txtMaxPrice.TabIndex = 36
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
        'frmPetDGandhiSettings
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(947, 524)
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
    Friend WithEvents lblMaxProfitPercentagePerDay As Label
    Friend WithEvents txtMaxLossPerDay As TextBox
    Friend WithEvents lblMaxLossPercentagePerDay As Label
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
    Friend WithEvents chbAllowToIncreaseCapital As CheckBox
    Friend WithEvents chbAutoSelectStock As CheckBox
    Friend WithEvents txtMinCapital As TextBox
    Friend WithEvents lblFutureMinCapital As Label
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
    Friend WithEvents txtMaxLossPercentagePerStock As TextBox
    Friend WithEvents lblMaxLossPercentagePerStock As Label
    Friend WithEvents txtPinbarTalePercentage As TextBox
    Friend WithEvents lblPinbarTalePercentage As Label
    Friend WithEvents txtMinLossPercentagePerTrade As TextBox
    Friend WithEvents lblMinLossPercentagePerTrade As Label
    Friend WithEvents txtMaxLossPercentagePerTrade As TextBox
    Friend WithEvents lblMaxLossPercentagePerTrade As Label
End Class
