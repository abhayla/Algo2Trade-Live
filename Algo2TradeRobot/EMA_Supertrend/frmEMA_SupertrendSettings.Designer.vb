<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmEMA_SupertrendSettings
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmEMA_SupertrendSettings))
        Me.opnFileSettings = New System.Windows.Forms.OpenFileDialog()
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
        Me.ImageList1 = New System.Windows.Forms.ImageList(Me.components)
        Me.btnBrowse = New System.Windows.Forms.Button()
        Me.txtInstrumentDetalis = New System.Windows.Forms.TextBox()
        Me.lblInstrumentDetails = New System.Windows.Forms.Label()
        Me.txtSignalTimeFrame = New System.Windows.Forms.TextBox()
        Me.lblSignalTimeFrame = New System.Windows.Forms.Label()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.btnSaveEMA_SupertrendSettings = New System.Windows.Forms.Button()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.txtSupertrendMultiplier = New System.Windows.Forms.TextBox()
        Me.lblSupertrendMultiplier = New System.Windows.Forms.Label()
        Me.txtSupertrendPeriod = New System.Windows.Forms.TextBox()
        Me.lblSupertrendPeriod = New System.Windows.Forms.Label()
        Me.txtSlowEMAPeriod = New System.Windows.Forms.TextBox()
        Me.lblSlowEMAPeriod = New System.Windows.Forms.Label()
        Me.txtFastEMAPeriod = New System.Windows.Forms.TextBox()
        Me.lblFastEMAPeriod = New System.Windows.Forms.Label()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.SuspendLayout()
        '
        'opnFileSettings
        '
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
        'ImageList1
        '
        Me.ImageList1.ImageStream = CType(resources.GetObject("ImageList1.ImageStream"), System.Windows.Forms.ImageListStreamer)
        Me.ImageList1.TransparentColor = System.Drawing.Color.Transparent
        Me.ImageList1.Images.SetKeyName(0, "save-icon-36533.png")
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
        Me.GroupBox1.Location = New System.Drawing.Point(4, 6)
        Me.GroupBox1.Margin = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Padding = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Size = New System.Drawing.Size(451, 271)
        Me.GroupBox1.TabIndex = 12
        Me.GroupBox1.TabStop = False
        '
        'btnSaveEMA_SupertrendSettings
        '
        Me.btnSaveEMA_SupertrendSettings.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnSaveEMA_SupertrendSettings.ImageKey = "save-icon-36533.png"
        Me.btnSaveEMA_SupertrendSettings.ImageList = Me.ImageList1
        Me.btnSaveEMA_SupertrendSettings.Location = New System.Drawing.Point(760, 13)
        Me.btnSaveEMA_SupertrendSettings.Margin = New System.Windows.Forms.Padding(4)
        Me.btnSaveEMA_SupertrendSettings.Name = "btnSaveEMA_SupertrendSettings"
        Me.btnSaveEMA_SupertrendSettings.Size = New System.Drawing.Size(112, 58)
        Me.btnSaveEMA_SupertrendSettings.TabIndex = 9
        Me.btnSaveEMA_SupertrendSettings.Text = "&Save"
        Me.btnSaveEMA_SupertrendSettings.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnSaveEMA_SupertrendSettings.UseVisualStyleBackColor = True
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.txtSupertrendMultiplier)
        Me.GroupBox2.Controls.Add(Me.lblSupertrendMultiplier)
        Me.GroupBox2.Controls.Add(Me.txtSupertrendPeriod)
        Me.GroupBox2.Controls.Add(Me.lblSupertrendPeriod)
        Me.GroupBox2.Controls.Add(Me.txtSlowEMAPeriod)
        Me.GroupBox2.Controls.Add(Me.lblSlowEMAPeriod)
        Me.GroupBox2.Controls.Add(Me.txtFastEMAPeriod)
        Me.GroupBox2.Controls.Add(Me.lblFastEMAPeriod)
        Me.GroupBox2.Location = New System.Drawing.Point(463, 6)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(289, 271)
        Me.GroupBox2.TabIndex = 13
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Indicator Settings"
        '
        'txtSupertrendMultiplier
        '
        Me.txtSupertrendMultiplier.Location = New System.Drawing.Point(156, 125)
        Me.txtSupertrendMultiplier.Margin = New System.Windows.Forms.Padding(4)
        Me.txtSupertrendMultiplier.Name = "txtSupertrendMultiplier"
        Me.txtSupertrendMultiplier.Size = New System.Drawing.Size(119, 22)
        Me.txtSupertrendMultiplier.TabIndex = 34
        '
        'lblSupertrendMultiplier
        '
        Me.lblSupertrendMultiplier.AutoSize = True
        Me.lblSupertrendMultiplier.Location = New System.Drawing.Point(9, 129)
        Me.lblSupertrendMultiplier.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblSupertrendMultiplier.Name = "lblSupertrendMultiplier"
        Me.lblSupertrendMultiplier.Size = New System.Drawing.Size(139, 17)
        Me.lblSupertrendMultiplier.TabIndex = 37
        Me.lblSupertrendMultiplier.Text = "Supertrend Multiplier"
        '
        'txtSupertrendPeriod
        '
        Me.txtSupertrendPeriod.Location = New System.Drawing.Point(156, 91)
        Me.txtSupertrendPeriod.Margin = New System.Windows.Forms.Padding(4)
        Me.txtSupertrendPeriod.Name = "txtSupertrendPeriod"
        Me.txtSupertrendPeriod.Size = New System.Drawing.Size(119, 22)
        Me.txtSupertrendPeriod.TabIndex = 33
        '
        'lblSupertrendPeriod
        '
        Me.lblSupertrendPeriod.AutoSize = True
        Me.lblSupertrendPeriod.Location = New System.Drawing.Point(9, 95)
        Me.lblSupertrendPeriod.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblSupertrendPeriod.Name = "lblSupertrendPeriod"
        Me.lblSupertrendPeriod.Size = New System.Drawing.Size(124, 17)
        Me.lblSupertrendPeriod.TabIndex = 36
        Me.lblSupertrendPeriod.Text = "Supertrend Period"
        '
        'txtSlowEMAPeriod
        '
        Me.txtSlowEMAPeriod.Location = New System.Drawing.Point(156, 56)
        Me.txtSlowEMAPeriod.Margin = New System.Windows.Forms.Padding(4)
        Me.txtSlowEMAPeriod.Name = "txtSlowEMAPeriod"
        Me.txtSlowEMAPeriod.Size = New System.Drawing.Size(119, 22)
        Me.txtSlowEMAPeriod.TabIndex = 32
        '
        'lblSlowEMAPeriod
        '
        Me.lblSlowEMAPeriod.AutoSize = True
        Me.lblSlowEMAPeriod.Location = New System.Drawing.Point(9, 60)
        Me.lblSlowEMAPeriod.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblSlowEMAPeriod.Name = "lblSlowEMAPeriod"
        Me.lblSlowEMAPeriod.Size = New System.Drawing.Size(115, 17)
        Me.lblSlowEMAPeriod.TabIndex = 35
        Me.lblSlowEMAPeriod.Text = "Slow EMA Period"
        '
        'txtFastEMAPeriod
        '
        Me.txtFastEMAPeriod.Location = New System.Drawing.Point(157, 24)
        Me.txtFastEMAPeriod.Margin = New System.Windows.Forms.Padding(4)
        Me.txtFastEMAPeriod.Name = "txtFastEMAPeriod"
        Me.txtFastEMAPeriod.Size = New System.Drawing.Size(119, 22)
        Me.txtFastEMAPeriod.TabIndex = 30
        '
        'lblFastEMAPeriod
        '
        Me.lblFastEMAPeriod.AutoSize = True
        Me.lblFastEMAPeriod.Location = New System.Drawing.Point(10, 28)
        Me.lblFastEMAPeriod.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblFastEMAPeriod.Name = "lblFastEMAPeriod"
        Me.lblFastEMAPeriod.Size = New System.Drawing.Size(113, 17)
        Me.lblFastEMAPeriod.TabIndex = 31
        Me.lblFastEMAPeriod.Text = "Fast EMA Period"
        '
        'frmEMA_SupertrendSettings
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(878, 282)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.btnSaveEMA_SupertrendSettings)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "frmEMA_SupertrendSettings"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "EMA & Supertrend Strategy - Settings"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents opnFileSettings As OpenFileDialog
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
    Friend WithEvents ImageList1 As ImageList
    Friend WithEvents btnBrowse As Button
    Friend WithEvents txtInstrumentDetalis As TextBox
    Friend WithEvents lblInstrumentDetails As Label
    Friend WithEvents txtSignalTimeFrame As TextBox
    Friend WithEvents lblSignalTimeFrame As Label
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents btnSaveEMA_SupertrendSettings As Button
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents txtSupertrendMultiplier As TextBox
    Friend WithEvents lblSupertrendMultiplier As Label
    Friend WithEvents txtSupertrendPeriod As TextBox
    Friend WithEvents lblSupertrendPeriod As Label
    Friend WithEvents txtSlowEMAPeriod As TextBox
    Friend WithEvents lblSlowEMAPeriod As Label
    Friend WithEvents txtFastEMAPeriod As TextBox
    Friend WithEvents lblFastEMAPeriod As Label
End Class
