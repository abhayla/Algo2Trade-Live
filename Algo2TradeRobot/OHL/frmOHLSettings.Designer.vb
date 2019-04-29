<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmOHLSettings
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmOHLSettings))
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
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.txtStoplossPercentage = New System.Windows.Forms.TextBox()
        Me.lblStoplossPercentage = New System.Windows.Forms.Label()
        Me.txtTargetPercentage = New System.Windows.Forms.TextBox()
        Me.lblTargetPercentage = New System.Windows.Forms.Label()
        Me.btnSaveOHLSettings = New System.Windows.Forms.Button()
        Me.GroupBox1.SuspendLayout()
        Me.SuspendLayout()
        '
        'opnFileSettings
        '
        '
        'txtMaxProfitPercentagePerDay
        '
        Me.txtMaxProfitPercentagePerDay.Location = New System.Drawing.Point(174, 159)
        Me.txtMaxProfitPercentagePerDay.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxProfitPercentagePerDay.Name = "txtMaxProfitPercentagePerDay"
        Me.txtMaxProfitPercentagePerDay.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxProfitPercentagePerDay.TabIndex = 4
        '
        'lblMaxProfitPercentagePerDay
        '
        Me.lblMaxProfitPercentagePerDay.AutoSize = True
        Me.lblMaxProfitPercentagePerDay.Location = New System.Drawing.Point(8, 163)
        Me.lblMaxProfitPercentagePerDay.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxProfitPercentagePerDay.Name = "lblMaxProfitPercentagePerDay"
        Me.lblMaxProfitPercentagePerDay.Size = New System.Drawing.Size(141, 17)
        Me.lblMaxProfitPercentagePerDay.TabIndex = 27
        Me.lblMaxProfitPercentagePerDay.Text = "Max Profit % Per Day"
        '
        'txtMaxLossPercentagePerDay
        '
        Me.txtMaxLossPercentagePerDay.Location = New System.Drawing.Point(174, 124)
        Me.txtMaxLossPercentagePerDay.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxLossPercentagePerDay.Name = "txtMaxLossPercentagePerDay"
        Me.txtMaxLossPercentagePerDay.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxLossPercentagePerDay.TabIndex = 3
        '
        'lblMaxLossPercentagePerDay
        '
        Me.lblMaxLossPercentagePerDay.AutoSize = True
        Me.lblMaxLossPercentagePerDay.Location = New System.Drawing.Point(8, 128)
        Me.lblMaxLossPercentagePerDay.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxLossPercentagePerDay.Name = "lblMaxLossPercentagePerDay"
        Me.lblMaxLossPercentagePerDay.Size = New System.Drawing.Size(138, 17)
        Me.lblMaxLossPercentagePerDay.TabIndex = 25
        Me.lblMaxLossPercentagePerDay.Text = "Max Loss % Per Day"
        '
        'dtpckrEODExitTime
        '
        Me.dtpckrEODExitTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrEODExitTime.Location = New System.Drawing.Point(175, 88)
        Me.dtpckrEODExitTime.Name = "dtpckrEODExitTime"
        Me.dtpckrEODExitTime.ShowUpDown = True
        Me.dtpckrEODExitTime.Size = New System.Drawing.Size(255, 22)
        Me.dtpckrEODExitTime.TabIndex = 2
        Me.dtpckrEODExitTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'dtpckrLastTradeEntryTime
        '
        Me.dtpckrLastTradeEntryTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrLastTradeEntryTime.Location = New System.Drawing.Point(176, 53)
        Me.dtpckrLastTradeEntryTime.Name = "dtpckrLastTradeEntryTime"
        Me.dtpckrLastTradeEntryTime.ShowUpDown = True
        Me.dtpckrLastTradeEntryTime.Size = New System.Drawing.Size(255, 22)
        Me.dtpckrLastTradeEntryTime.TabIndex = 1
        Me.dtpckrLastTradeEntryTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'dtpckrTradeStartTime
        '
        Me.dtpckrTradeStartTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrTradeStartTime.Location = New System.Drawing.Point(175, 16)
        Me.dtpckrTradeStartTime.Name = "dtpckrTradeStartTime"
        Me.dtpckrTradeStartTime.ShowUpDown = True
        Me.dtpckrTradeStartTime.Size = New System.Drawing.Size(255, 22)
        Me.dtpckrTradeStartTime.TabIndex = 0
        Me.dtpckrTradeStartTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'lblEODExitTime
        '
        Me.lblEODExitTime.AutoSize = True
        Me.lblEODExitTime.Location = New System.Drawing.Point(9, 89)
        Me.lblEODExitTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblEODExitTime.Name = "lblEODExitTime"
        Me.lblEODExitTime.Size = New System.Drawing.Size(99, 17)
        Me.lblEODExitTime.TabIndex = 23
        Me.lblEODExitTime.Text = "EOD Exit Time"
        '
        'lblLastTradeEntryTime
        '
        Me.lblLastTradeEntryTime.AutoSize = True
        Me.lblLastTradeEntryTime.Location = New System.Drawing.Point(9, 54)
        Me.lblLastTradeEntryTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblLastTradeEntryTime.Name = "lblLastTradeEntryTime"
        Me.lblLastTradeEntryTime.Size = New System.Drawing.Size(149, 17)
        Me.lblLastTradeEntryTime.TabIndex = 21
        Me.lblLastTradeEntryTime.Text = "Last Trade Entry Time"
        '
        'lblTradeStartTime
        '
        Me.lblTradeStartTime.AutoSize = True
        Me.lblTradeStartTime.Location = New System.Drawing.Point(9, 18)
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
        Me.btnBrowse.Location = New System.Drawing.Point(404, 258)
        Me.btnBrowse.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.btnBrowse.Name = "btnBrowse"
        Me.btnBrowse.Size = New System.Drawing.Size(40, 23)
        Me.btnBrowse.TabIndex = 7
        Me.btnBrowse.Text = "..."
        Me.btnBrowse.UseVisualStyleBackColor = True
        '
        'txtInstrumentDetalis
        '
        Me.txtInstrumentDetalis.Location = New System.Drawing.Point(174, 259)
        Me.txtInstrumentDetalis.Margin = New System.Windows.Forms.Padding(4)
        Me.txtInstrumentDetalis.Name = "txtInstrumentDetalis"
        Me.txtInstrumentDetalis.ReadOnly = True
        Me.txtInstrumentDetalis.Size = New System.Drawing.Size(223, 22)
        Me.txtInstrumentDetalis.TabIndex = 15
        '
        'lblInstrumentDetails
        '
        Me.lblInstrumentDetails.AutoSize = True
        Me.lblInstrumentDetails.Location = New System.Drawing.Point(8, 262)
        Me.lblInstrumentDetails.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblInstrumentDetails.Name = "lblInstrumentDetails"
        Me.lblInstrumentDetails.Size = New System.Drawing.Size(121, 17)
        Me.lblInstrumentDetails.TabIndex = 8
        Me.lblInstrumentDetails.Text = "Instrument Details"
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.txtStoplossPercentage)
        Me.GroupBox1.Controls.Add(Me.lblStoplossPercentage)
        Me.GroupBox1.Controls.Add(Me.txtTargetPercentage)
        Me.GroupBox1.Controls.Add(Me.lblTargetPercentage)
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
        Me.GroupBox1.Location = New System.Drawing.Point(4, 6)
        Me.GroupBox1.Margin = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Padding = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Size = New System.Drawing.Size(451, 294)
        Me.GroupBox1.TabIndex = 11
        Me.GroupBox1.TabStop = False
        '
        'txtStoplossPercentage
        '
        Me.txtStoplossPercentage.Location = New System.Drawing.Point(174, 192)
        Me.txtStoplossPercentage.Margin = New System.Windows.Forms.Padding(4)
        Me.txtStoplossPercentage.Name = "txtStoplossPercentage"
        Me.txtStoplossPercentage.Size = New System.Drawing.Size(256, 22)
        Me.txtStoplossPercentage.TabIndex = 5
        '
        'lblStoplossPercentage
        '
        Me.lblStoplossPercentage.AutoSize = True
        Me.lblStoplossPercentage.Location = New System.Drawing.Point(8, 196)
        Me.lblStoplossPercentage.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblStoplossPercentage.Name = "lblStoplossPercentage"
        Me.lblStoplossPercentage.Size = New System.Drawing.Size(78, 17)
        Me.lblStoplossPercentage.TabIndex = 30
        Me.lblStoplossPercentage.Text = "Stoploss %"
        '
        'txtTargetPercentage
        '
        Me.txtTargetPercentage.Location = New System.Drawing.Point(174, 226)
        Me.txtTargetPercentage.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTargetPercentage.Name = "txtTargetPercentage"
        Me.txtTargetPercentage.Size = New System.Drawing.Size(256, 22)
        Me.txtTargetPercentage.TabIndex = 6
        '
        'lblTargetPercentage
        '
        Me.lblTargetPercentage.AutoSize = True
        Me.lblTargetPercentage.Location = New System.Drawing.Point(8, 230)
        Me.lblTargetPercentage.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTargetPercentage.Name = "lblTargetPercentage"
        Me.lblTargetPercentage.Size = New System.Drawing.Size(66, 17)
        Me.lblTargetPercentage.TabIndex = 28
        Me.lblTargetPercentage.Text = "Target %"
        '
        'btnSaveOHLSettings
        '
        Me.btnSaveOHLSettings.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnSaveOHLSettings.ImageKey = "save-icon-36533.png"
        Me.btnSaveOHLSettings.ImageList = Me.ImageList1
        Me.btnSaveOHLSettings.Location = New System.Drawing.Point(459, 13)
        Me.btnSaveOHLSettings.Margin = New System.Windows.Forms.Padding(4)
        Me.btnSaveOHLSettings.Name = "btnSaveOHLSettings"
        Me.btnSaveOHLSettings.Size = New System.Drawing.Size(112, 58)
        Me.btnSaveOHLSettings.TabIndex = 8
        Me.btnSaveOHLSettings.Text = "&Save"
        Me.btnSaveOHLSettings.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnSaveOHLSettings.UseVisualStyleBackColor = True
        '
        'frmOHLSettings
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(575, 308)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.btnSaveOHLSettings)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "frmOHLSettings"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "OHL - Settings"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
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
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents btnSaveOHLSettings As Button
    Friend WithEvents txtTargetPercentage As TextBox
    Friend WithEvents lblTargetPercentage As Label
    Friend WithEvents txtStoplossPercentage As TextBox
    Friend WithEvents lblStoplossPercentage As Label
End Class
