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
        Me.grpTelegram = New System.Windows.Forms.GroupBox()
        Me.txtTelegramChatIDForPL = New System.Windows.Forms.TextBox()
        Me.lblChatIDForPL = New System.Windows.Forms.Label()
        Me.txtTelegramChatID = New System.Windows.Forms.TextBox()
        Me.lblTelegramChatID = New System.Windows.Forms.Label()
        Me.txtTelegramAPI = New System.Windows.Forms.TextBox()
        Me.lblTelegramAPI = New System.Windows.Forms.Label()
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
        Me.lblTargetMultiplier = New System.Windows.Forms.Label()
        Me.txtNumberOfTradePerStock = New System.Windows.Forms.TextBox()
        Me.lblNumberOfTradePerStock = New System.Windows.Forms.Label()
        Me.dtpckrTradeStartTime = New System.Windows.Forms.DateTimePicker()
        Me.lblLastTradeEntryTime = New System.Windows.Forms.Label()
        Me.lblTradeStartTime = New System.Windows.Forms.Label()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.chbReverseTrade = New System.Windows.Forms.CheckBox()
        Me.chbStoplossMoveToBreakeven = New System.Windows.Forms.CheckBox()
        Me.chbCountTradesWithBreakevenMovement = New System.Windows.Forms.CheckBox()
        Me.grpTelegram.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
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
        'grpTelegram
        '
        Me.grpTelegram.Controls.Add(Me.txtTelegramChatIDForPL)
        Me.grpTelegram.Controls.Add(Me.lblChatIDForPL)
        Me.grpTelegram.Controls.Add(Me.txtTelegramChatID)
        Me.grpTelegram.Controls.Add(Me.lblTelegramChatID)
        Me.grpTelegram.Controls.Add(Me.txtTelegramAPI)
        Me.grpTelegram.Controls.Add(Me.lblTelegramAPI)
        Me.grpTelegram.Location = New System.Drawing.Point(493, 134)
        Me.grpTelegram.Name = "grpTelegram"
        Me.grpTelegram.Size = New System.Drawing.Size(358, 122)
        Me.grpTelegram.TabIndex = 29
        Me.grpTelegram.TabStop = False
        Me.grpTelegram.Text = "Telegram Details"
        '
        'txtTelegramChatIDForPL
        '
        Me.txtTelegramChatIDForPL.Location = New System.Drawing.Point(94, 88)
        Me.txtTelegramChatIDForPL.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTelegramChatIDForPL.Name = "txtTelegramChatIDForPL"
        Me.txtTelegramChatIDForPL.Size = New System.Drawing.Size(253, 22)
        Me.txtTelegramChatIDForPL.TabIndex = 12
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
        'txtTelegramChatID
        '
        Me.txtTelegramChatID.Location = New System.Drawing.Point(94, 57)
        Me.txtTelegramChatID.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTelegramChatID.Name = "txtTelegramChatID"
        Me.txtTelegramChatID.Size = New System.Drawing.Size(255, 22)
        Me.txtTelegramChatID.TabIndex = 11
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
        Me.txtTelegramAPI.Location = New System.Drawing.Point(94, 25)
        Me.txtTelegramAPI.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTelegramAPI.Name = "txtTelegramAPI"
        Me.txtTelegramAPI.Size = New System.Drawing.Size(255, 22)
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
        Me.btnBrowse.Location = New System.Drawing.Point(428, 264)
        Me.btnBrowse.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.btnBrowse.Name = "btnBrowse"
        Me.btnBrowse.Size = New System.Drawing.Size(40, 23)
        Me.btnBrowse.TabIndex = 8
        Me.btnBrowse.Text = "..."
        Me.btnBrowse.UseVisualStyleBackColor = True
        '
        'txtInstrumentDetalis
        '
        Me.txtInstrumentDetalis.Location = New System.Drawing.Point(198, 265)
        Me.txtInstrumentDetalis.Margin = New System.Windows.Forms.Padding(4)
        Me.txtInstrumentDetalis.Name = "txtInstrumentDetalis"
        Me.txtInstrumentDetalis.ReadOnly = True
        Me.txtInstrumentDetalis.Size = New System.Drawing.Size(223, 22)
        Me.txtInstrumentDetalis.TabIndex = 15
        '
        'lblInstrumentDetails
        '
        Me.lblInstrumentDetails.AutoSize = True
        Me.lblInstrumentDetails.Location = New System.Drawing.Point(8, 268)
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
        Me.GroupBox1.Size = New System.Drawing.Size(477, 300)
        Me.GroupBox1.TabIndex = 28
        Me.GroupBox1.TabStop = False
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
        Me.GroupBox2.Size = New System.Drawing.Size(358, 119)
        Me.GroupBox2.TabIndex = 30
        Me.GroupBox2.TabStop = False
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
        'frmTwoThirdSettings
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(975, 309)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.grpTelegram)
        Me.Controls.Add(Me.btnTwoThirdStrayegySettings)
        Me.Controls.Add(Me.GroupBox1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "frmTwoThirdSettings"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "TwoThird Strategy - Settings"
        Me.grpTelegram.ResumeLayout(False)
        Me.grpTelegram.PerformLayout()
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents ImageList1 As ImageList
    Friend WithEvents opnFileSettings As OpenFileDialog
    Friend WithEvents grpTelegram As GroupBox
    Friend WithEvents txtTelegramChatIDForPL As TextBox
    Friend WithEvents lblChatIDForPL As Label
    Friend WithEvents txtTelegramChatID As TextBox
    Friend WithEvents lblTelegramChatID As Label
    Friend WithEvents txtTelegramAPI As TextBox
    Friend WithEvents lblTelegramAPI As Label
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
End Class
