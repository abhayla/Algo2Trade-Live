<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmNearFarHedgingSettings
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmNearFarHedgingSettings))
        Me.grpIndicator = New System.Windows.Forms.GroupBox()
        Me.txtBollingerMultiplier = New System.Windows.Forms.TextBox()
        Me.lblBollingerMultiplier = New System.Windows.Forms.Label()
        Me.txtBollingerPeriod = New System.Windows.Forms.TextBox()
        Me.lblBollingerPeriod = New System.Windows.Forms.Label()
        Me.txtMaxProfitPercentagePerDay = New System.Windows.Forms.TextBox()
        Me.lblMaxProfitPercentagePerDay = New System.Windows.Forms.Label()
        Me.txtMaxLossPercentagePerDay = New System.Windows.Forms.TextBox()
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
        Me.lblInstrumentDetails = New System.Windows.Forms.Label()
        Me.txtSignalTimeFrame = New System.Windows.Forms.TextBox()
        Me.lblSignalTimeFrame = New System.Windows.Forms.Label()
        Me.ImageList1 = New System.Windows.Forms.ImageList(Me.components)
        Me.btnSaveNearFarHedgingSettings = New System.Windows.Forms.Button()
        Me.opnFileSettings = New System.Windows.Forms.OpenFileDialog()
        Me.grpSignal = New System.Windows.Forms.GroupBox()
        Me.rdbBoth = New System.Windows.Forms.RadioButton()
        Me.rdbAnyOne = New System.Windows.Forms.RadioButton()
        Me.grpTelegram = New System.Windows.Forms.GroupBox()
        Me.txtTelegramChatID = New System.Windows.Forms.TextBox()
        Me.lblTelegramChatID = New System.Windows.Forms.Label()
        Me.txtTelegramAPI = New System.Windows.Forms.TextBox()
        Me.lblTelegramAPI = New System.Windows.Forms.Label()
        Me.txtTelegramChatIDForPL = New System.Windows.Forms.TextBox()
        Me.lblChatIDForPL = New System.Windows.Forms.Label()
        Me.grpIndicator.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.grpSignal.SuspendLayout()
        Me.grpTelegram.SuspendLayout()
        Me.SuspendLayout()
        '
        'grpIndicator
        '
        Me.grpIndicator.Controls.Add(Me.txtBollingerMultiplier)
        Me.grpIndicator.Controls.Add(Me.lblBollingerMultiplier)
        Me.grpIndicator.Controls.Add(Me.txtBollingerPeriod)
        Me.grpIndicator.Controls.Add(Me.lblBollingerPeriod)
        Me.grpIndicator.Location = New System.Drawing.Point(464, 6)
        Me.grpIndicator.Name = "grpIndicator"
        Me.grpIndicator.Size = New System.Drawing.Size(289, 95)
        Me.grpIndicator.TabIndex = 16
        Me.grpIndicator.TabStop = False
        Me.grpIndicator.Text = "Indicator Settings"
        '
        'txtBollingerMultiplier
        '
        Me.txtBollingerMultiplier.Location = New System.Drawing.Point(156, 56)
        Me.txtBollingerMultiplier.Margin = New System.Windows.Forms.Padding(4)
        Me.txtBollingerMultiplier.Name = "txtBollingerMultiplier"
        Me.txtBollingerMultiplier.Size = New System.Drawing.Size(119, 22)
        Me.txtBollingerMultiplier.TabIndex = 32
        '
        'lblBollingerMultiplier
        '
        Me.lblBollingerMultiplier.AutoSize = True
        Me.lblBollingerMultiplier.Location = New System.Drawing.Point(9, 60)
        Me.lblBollingerMultiplier.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblBollingerMultiplier.Name = "lblBollingerMultiplier"
        Me.lblBollingerMultiplier.Size = New System.Drawing.Size(123, 17)
        Me.lblBollingerMultiplier.TabIndex = 35
        Me.lblBollingerMultiplier.Text = "Bollinger Multiplier"
        '
        'txtBollingerPeriod
        '
        Me.txtBollingerPeriod.Location = New System.Drawing.Point(157, 24)
        Me.txtBollingerPeriod.Margin = New System.Windows.Forms.Padding(4)
        Me.txtBollingerPeriod.Name = "txtBollingerPeriod"
        Me.txtBollingerPeriod.Size = New System.Drawing.Size(119, 22)
        Me.txtBollingerPeriod.TabIndex = 30
        '
        'lblBollingerPeriod
        '
        Me.lblBollingerPeriod.AutoSize = True
        Me.lblBollingerPeriod.Location = New System.Drawing.Point(10, 28)
        Me.lblBollingerPeriod.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblBollingerPeriod.Name = "lblBollingerPeriod"
        Me.lblBollingerPeriod.Size = New System.Drawing.Size(108, 17)
        Me.lblBollingerPeriod.TabIndex = 31
        Me.lblBollingerPeriod.Text = "Bollinger Period"
        '
        'txtMaxProfitPercentagePerDay
        '
        Me.txtMaxProfitPercentagePerDay.Location = New System.Drawing.Point(174, 202)
        Me.txtMaxProfitPercentagePerDay.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxProfitPercentagePerDay.Name = "txtMaxProfitPercentagePerDay"
        Me.txtMaxProfitPercentagePerDay.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxProfitPercentagePerDay.TabIndex = 6
        '
        'lblMaxProfitPercentagePerDay
        '
        Me.lblMaxProfitPercentagePerDay.AutoSize = True
        Me.lblMaxProfitPercentagePerDay.Location = New System.Drawing.Point(8, 206)
        Me.lblMaxProfitPercentagePerDay.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxProfitPercentagePerDay.Name = "lblMaxProfitPercentagePerDay"
        Me.lblMaxProfitPercentagePerDay.Size = New System.Drawing.Size(141, 17)
        Me.lblMaxProfitPercentagePerDay.TabIndex = 27
        Me.lblMaxProfitPercentagePerDay.Text = "Max Profit % Per Day"
        '
        'txtMaxLossPercentagePerDay
        '
        Me.txtMaxLossPercentagePerDay.Location = New System.Drawing.Point(174, 167)
        Me.txtMaxLossPercentagePerDay.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxLossPercentagePerDay.Name = "txtMaxLossPercentagePerDay"
        Me.txtMaxLossPercentagePerDay.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxLossPercentagePerDay.TabIndex = 5
        '
        'lblMaxLossPercentagePerDay
        '
        Me.lblMaxLossPercentagePerDay.AutoSize = True
        Me.lblMaxLossPercentagePerDay.Location = New System.Drawing.Point(8, 171)
        Me.lblMaxLossPercentagePerDay.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxLossPercentagePerDay.Name = "lblMaxLossPercentagePerDay"
        Me.lblMaxLossPercentagePerDay.Size = New System.Drawing.Size(138, 17)
        Me.lblMaxLossPercentagePerDay.TabIndex = 25
        Me.lblMaxLossPercentagePerDay.Text = "Max Loss % Per Day"
        '
        'dtpckrEODExitTime
        '
        Me.dtpckrEODExitTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrEODExitTime.Location = New System.Drawing.Point(175, 131)
        Me.dtpckrEODExitTime.Name = "dtpckrEODExitTime"
        Me.dtpckrEODExitTime.ShowUpDown = True
        Me.dtpckrEODExitTime.Size = New System.Drawing.Size(255, 22)
        Me.dtpckrEODExitTime.TabIndex = 3
        Me.dtpckrEODExitTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'dtpckrLastTradeEntryTime
        '
        Me.dtpckrLastTradeEntryTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrLastTradeEntryTime.Location = New System.Drawing.Point(176, 96)
        Me.dtpckrLastTradeEntryTime.Name = "dtpckrLastTradeEntryTime"
        Me.dtpckrLastTradeEntryTime.ShowUpDown = True
        Me.dtpckrLastTradeEntryTime.Size = New System.Drawing.Size(255, 22)
        Me.dtpckrLastTradeEntryTime.TabIndex = 2
        Me.dtpckrLastTradeEntryTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'dtpckrTradeStartTime
        '
        Me.dtpckrTradeStartTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrTradeStartTime.Location = New System.Drawing.Point(175, 59)
        Me.dtpckrTradeStartTime.Name = "dtpckrTradeStartTime"
        Me.dtpckrTradeStartTime.ShowUpDown = True
        Me.dtpckrTradeStartTime.Size = New System.Drawing.Size(255, 22)
        Me.dtpckrTradeStartTime.TabIndex = 1
        Me.dtpckrTradeStartTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'lblEODExitTime
        '
        Me.lblEODExitTime.AutoSize = True
        Me.lblEODExitTime.Location = New System.Drawing.Point(9, 132)
        Me.lblEODExitTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblEODExitTime.Name = "lblEODExitTime"
        Me.lblEODExitTime.Size = New System.Drawing.Size(99, 17)
        Me.lblEODExitTime.TabIndex = 23
        Me.lblEODExitTime.Text = "EOD Exit Time"
        '
        'lblLastTradeEntryTime
        '
        Me.lblLastTradeEntryTime.AutoSize = True
        Me.lblLastTradeEntryTime.Location = New System.Drawing.Point(9, 97)
        Me.lblLastTradeEntryTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblLastTradeEntryTime.Name = "lblLastTradeEntryTime"
        Me.lblLastTradeEntryTime.Size = New System.Drawing.Size(149, 17)
        Me.lblLastTradeEntryTime.TabIndex = 21
        Me.lblLastTradeEntryTime.Text = "Last Trade Entry Time"
        '
        'lblTradeStartTime
        '
        Me.lblTradeStartTime.AutoSize = True
        Me.lblTradeStartTime.Location = New System.Drawing.Point(9, 61)
        Me.lblTradeStartTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTradeStartTime.Name = "lblTradeStartTime"
        Me.lblTradeStartTime.Size = New System.Drawing.Size(115, 17)
        Me.lblTradeStartTime.TabIndex = 19
        Me.lblTradeStartTime.Text = "Trade Start Time"
        '
        'btnBrowse
        '
        Me.btnBrowse.Font = New System.Drawing.Font("Microsoft Sans Serif", 7.8!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnBrowse.Location = New System.Drawing.Point(404, 234)
        Me.btnBrowse.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.btnBrowse.Name = "btnBrowse"
        Me.btnBrowse.Size = New System.Drawing.Size(40, 23)
        Me.btnBrowse.TabIndex = 8
        Me.btnBrowse.Text = "..."
        Me.btnBrowse.UseVisualStyleBackColor = True
        '
        'txtInstrumentDetalis
        '
        Me.txtInstrumentDetalis.Location = New System.Drawing.Point(174, 235)
        Me.txtInstrumentDetalis.Margin = New System.Windows.Forms.Padding(4)
        Me.txtInstrumentDetalis.Name = "txtInstrumentDetalis"
        Me.txtInstrumentDetalis.ReadOnly = True
        Me.txtInstrumentDetalis.Size = New System.Drawing.Size(223, 22)
        Me.txtInstrumentDetalis.TabIndex = 15
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.txtMaxProfitPercentagePerDay)
        Me.GroupBox1.Controls.Add(Me.lblMaxProfitPercentagePerDay)
        Me.GroupBox1.Controls.Add(Me.txtMaxLossPercentagePerDay)
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
        Me.GroupBox1.Size = New System.Drawing.Size(451, 271)
        Me.GroupBox1.TabIndex = 15
        Me.GroupBox1.TabStop = False
        '
        'lblInstrumentDetails
        '
        Me.lblInstrumentDetails.AutoSize = True
        Me.lblInstrumentDetails.Location = New System.Drawing.Point(8, 238)
        Me.lblInstrumentDetails.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblInstrumentDetails.Name = "lblInstrumentDetails"
        Me.lblInstrumentDetails.Size = New System.Drawing.Size(121, 17)
        Me.lblInstrumentDetails.TabIndex = 8
        Me.lblInstrumentDetails.Text = "Instrument Details"
        '
        'txtSignalTimeFrame
        '
        Me.txtSignalTimeFrame.Location = New System.Drawing.Point(175, 21)
        Me.txtSignalTimeFrame.Margin = New System.Windows.Forms.Padding(4)
        Me.txtSignalTimeFrame.Name = "txtSignalTimeFrame"
        Me.txtSignalTimeFrame.Size = New System.Drawing.Size(255, 22)
        Me.txtSignalTimeFrame.TabIndex = 0
        '
        'lblSignalTimeFrame
        '
        Me.lblSignalTimeFrame.AutoSize = True
        Me.lblSignalTimeFrame.Location = New System.Drawing.Point(9, 25)
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
        'btnSaveNearFarHedgingSettings
        '
        Me.btnSaveNearFarHedgingSettings.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnSaveNearFarHedgingSettings.ImageKey = "save-icon-36533.png"
        Me.btnSaveNearFarHedgingSettings.ImageList = Me.ImageList1
        Me.btnSaveNearFarHedgingSettings.Location = New System.Drawing.Point(761, 13)
        Me.btnSaveNearFarHedgingSettings.Margin = New System.Windows.Forms.Padding(4)
        Me.btnSaveNearFarHedgingSettings.Name = "btnSaveNearFarHedgingSettings"
        Me.btnSaveNearFarHedgingSettings.Size = New System.Drawing.Size(112, 58)
        Me.btnSaveNearFarHedgingSettings.TabIndex = 14
        Me.btnSaveNearFarHedgingSettings.Text = "&Save"
        Me.btnSaveNearFarHedgingSettings.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnSaveNearFarHedgingSettings.UseVisualStyleBackColor = True
        '
        'opnFileSettings
        '
        '
        'grpSignal
        '
        Me.grpSignal.Controls.Add(Me.rdbBoth)
        Me.grpSignal.Controls.Add(Me.rdbAnyOne)
        Me.grpSignal.Location = New System.Drawing.Point(464, 101)
        Me.grpSignal.Name = "grpSignal"
        Me.grpSignal.Size = New System.Drawing.Size(289, 58)
        Me.grpSignal.TabIndex = 17
        Me.grpSignal.TabStop = False
        Me.grpSignal.Text = "Signal Type"
        '
        'rdbBoth
        '
        Me.rdbBoth.AutoSize = True
        Me.rdbBoth.Location = New System.Drawing.Point(105, 29)
        Me.rdbBoth.Name = "rdbBoth"
        Me.rdbBoth.Size = New System.Drawing.Size(58, 21)
        Me.rdbBoth.TabIndex = 1
        Me.rdbBoth.TabStop = True
        Me.rdbBoth.Text = "Both"
        Me.rdbBoth.UseVisualStyleBackColor = True
        '
        'rdbAnyOne
        '
        Me.rdbAnyOne.AutoSize = True
        Me.rdbAnyOne.Location = New System.Drawing.Point(13, 29)
        Me.rdbAnyOne.Name = "rdbAnyOne"
        Me.rdbAnyOne.Size = New System.Drawing.Size(77, 21)
        Me.rdbAnyOne.TabIndex = 0
        Me.rdbAnyOne.TabStop = True
        Me.rdbAnyOne.Text = "Anyone"
        Me.rdbAnyOne.UseVisualStyleBackColor = True
        '
        'grpTelegram
        '
        Me.grpTelegram.Controls.Add(Me.txtTelegramChatIDForPL)
        Me.grpTelegram.Controls.Add(Me.lblChatIDForPL)
        Me.grpTelegram.Controls.Add(Me.txtTelegramChatID)
        Me.grpTelegram.Controls.Add(Me.lblTelegramChatID)
        Me.grpTelegram.Controls.Add(Me.txtTelegramAPI)
        Me.grpTelegram.Controls.Add(Me.lblTelegramAPI)
        Me.grpTelegram.Location = New System.Drawing.Point(463, 159)
        Me.grpTelegram.Name = "grpTelegram"
        Me.grpTelegram.Size = New System.Drawing.Size(289, 118)
        Me.grpTelegram.TabIndex = 18
        Me.grpTelegram.TabStop = False
        Me.grpTelegram.Text = "Telegram Details"
        '
        'txtTelegramChatID
        '
        Me.txtTelegramChatID.Location = New System.Drawing.Point(92, 56)
        Me.txtTelegramChatID.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTelegramChatID.Name = "txtTelegramChatID"
        Me.txtTelegramChatID.Size = New System.Drawing.Size(183, 22)
        Me.txtTelegramChatID.TabIndex = 32
        '
        'lblTelegramChatID
        '
        Me.lblTelegramChatID.AutoSize = True
        Me.lblTelegramChatID.Location = New System.Drawing.Point(9, 60)
        Me.lblTelegramChatID.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTelegramChatID.Name = "lblTelegramChatID"
        Me.lblTelegramChatID.Size = New System.Drawing.Size(54, 17)
        Me.lblTelegramChatID.TabIndex = 35
        Me.lblTelegramChatID.Text = "Chat ID"
        '
        'txtTelegramAPI
        '
        Me.txtTelegramAPI.Location = New System.Drawing.Point(92, 24)
        Me.txtTelegramAPI.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTelegramAPI.Name = "txtTelegramAPI"
        Me.txtTelegramAPI.Size = New System.Drawing.Size(184, 22)
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
        'txtTelegramChatIDForPL
        '
        Me.txtTelegramChatIDForPL.Location = New System.Drawing.Point(92, 87)
        Me.txtTelegramChatIDForPL.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTelegramChatIDForPL.Name = "txtTelegramChatIDForPL"
        Me.txtTelegramChatIDForPL.Size = New System.Drawing.Size(183, 22)
        Me.txtTelegramChatIDForPL.TabIndex = 36
        '
        'lblChatIDForPL
        '
        Me.lblChatIDForPL.AutoSize = True
        Me.lblChatIDForPL.Location = New System.Drawing.Point(9, 91)
        Me.lblChatIDForPL.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblChatIDForPL.Name = "lblChatIDForPL"
        Me.lblChatIDForPL.Size = New System.Drawing.Size(75, 17)
        Me.lblChatIDForPL.TabIndex = 37
        Me.lblChatIDForPL.Text = "PL Chat ID"
        '
        'frmNearFarHedgingSettings
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(878, 282)
        Me.Controls.Add(Me.grpTelegram)
        Me.Controls.Add(Me.grpSignal)
        Me.Controls.Add(Me.grpIndicator)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.btnSaveNearFarHedgingSettings)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "frmNearFarHedgingSettings"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Near Far Hedging Strategy - Settings"
        Me.grpIndicator.ResumeLayout(False)
        Me.grpIndicator.PerformLayout()
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.grpSignal.ResumeLayout(False)
        Me.grpSignal.PerformLayout()
        Me.grpTelegram.ResumeLayout(False)
        Me.grpTelegram.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents grpIndicator As GroupBox
    Friend WithEvents txtBollingerMultiplier As TextBox
    Friend WithEvents lblBollingerMultiplier As Label
    Friend WithEvents txtBollingerPeriod As TextBox
    Friend WithEvents lblBollingerPeriod As Label
    Friend WithEvents txtMaxProfitPercentagePerDay As TextBox
    Friend WithEvents lblMaxProfitPercentagePerDay As Label
    Friend WithEvents txtMaxLossPercentagePerDay As TextBox
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
    Friend WithEvents btnSaveNearFarHedgingSettings As Button
    Friend WithEvents opnFileSettings As OpenFileDialog
    Friend WithEvents grpSignal As GroupBox
    Friend WithEvents rdbBoth As RadioButton
    Friend WithEvents rdbAnyOne As RadioButton
    Friend WithEvents grpTelegram As GroupBox
    Friend WithEvents txtTelegramChatID As TextBox
    Friend WithEvents lblTelegramChatID As Label
    Friend WithEvents txtTelegramAPI As TextBox
    Friend WithEvents lblTelegramAPI As Label
    Friend WithEvents txtTelegramChatIDForPL As TextBox
    Friend WithEvents lblChatIDForPL As Label
End Class
