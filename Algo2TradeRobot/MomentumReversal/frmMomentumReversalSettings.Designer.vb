<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class frmMomentumReversalSettings
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmMomentumReversalSettings))
        Me.btnSaveMomentumReversalSettings = New System.Windows.Forms.Button()
        Me.ImageList1 = New System.Windows.Forms.ImageList(Me.components)
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
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
        Me.txtMinCandleRangePercentage = New System.Windows.Forms.TextBox()
        Me.lblMinCandleRangePercentage = New System.Windows.Forms.Label()
        Me.btnBrowse = New System.Windows.Forms.Button()
        Me.txtInstrumentDetalis = New System.Windows.Forms.TextBox()
        Me.lblInstrumentDetails = New System.Windows.Forms.Label()
        Me.txtSignalTimeFrame = New System.Windows.Forms.TextBox()
        Me.txtTargetMultiplier = New System.Windows.Forms.TextBox()
        Me.txtCapitalProtectionPercentage = New System.Windows.Forms.TextBox()
        Me.txtCandleWickSizePercentage = New System.Windows.Forms.TextBox()
        Me.lblSignalTimeFrame = New System.Windows.Forms.Label()
        Me.lblTargetMultiplier = New System.Windows.Forms.Label()
        Me.lblCapitalProtectionPercentage = New System.Windows.Forms.Label()
        Me.lblCandleWickSizePercentage = New System.Windows.Forms.Label()
        Me.opnFileSettings = New System.Windows.Forms.OpenFileDialog()
        Me.GroupBox1.SuspendLayout()
        Me.SuspendLayout()
        '
        'btnSaveMomentumReversalSettings
        '
        Me.btnSaveMomentumReversalSettings.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnSaveMomentumReversalSettings.ImageKey = "save-icon-36533.png"
        Me.btnSaveMomentumReversalSettings.ImageList = Me.ImageList1
        Me.btnSaveMomentumReversalSettings.Location = New System.Drawing.Point(459, 11)
        Me.btnSaveMomentumReversalSettings.Margin = New System.Windows.Forms.Padding(4)
        Me.btnSaveMomentumReversalSettings.Name = "btnSaveMomentumReversalSettings"
        Me.btnSaveMomentumReversalSettings.Size = New System.Drawing.Size(112, 58)
        Me.btnSaveMomentumReversalSettings.TabIndex = 11
        Me.btnSaveMomentumReversalSettings.Text = "&Save"
        Me.btnSaveMomentumReversalSettings.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnSaveMomentumReversalSettings.UseVisualStyleBackColor = True
        '
        'ImageList1
        '
        Me.ImageList1.ImageStream = CType(resources.GetObject("ImageList1.ImageStream"), System.Windows.Forms.ImageListStreamer)
        Me.ImageList1.TransparentColor = System.Drawing.Color.Transparent
        Me.ImageList1.Images.SetKeyName(0, "save-icon-36533.png")
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
        Me.GroupBox1.Controls.Add(Me.txtMinCandleRangePercentage)
        Me.GroupBox1.Controls.Add(Me.lblMinCandleRangePercentage)
        Me.GroupBox1.Controls.Add(Me.btnBrowse)
        Me.GroupBox1.Controls.Add(Me.txtInstrumentDetalis)
        Me.GroupBox1.Controls.Add(Me.lblInstrumentDetails)
        Me.GroupBox1.Controls.Add(Me.txtSignalTimeFrame)
        Me.GroupBox1.Controls.Add(Me.txtTargetMultiplier)
        Me.GroupBox1.Controls.Add(Me.txtCapitalProtectionPercentage)
        Me.GroupBox1.Controls.Add(Me.txtCandleWickSizePercentage)
        Me.GroupBox1.Controls.Add(Me.lblSignalTimeFrame)
        Me.GroupBox1.Controls.Add(Me.lblTargetMultiplier)
        Me.GroupBox1.Controls.Add(Me.lblCapitalProtectionPercentage)
        Me.GroupBox1.Controls.Add(Me.lblCandleWickSizePercentage)
        Me.GroupBox1.Location = New System.Drawing.Point(4, 4)
        Me.GroupBox1.Margin = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Padding = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Size = New System.Drawing.Size(451, 426)
        Me.GroupBox1.TabIndex = 0
        Me.GroupBox1.TabStop = False
        '
        'txtMaxProfitPercentagePerDay
        '
        Me.txtMaxProfitPercentagePerDay.Location = New System.Drawing.Point(174, 237)
        Me.txtMaxProfitPercentagePerDay.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxProfitPercentagePerDay.Name = "txtMaxProfitPercentagePerDay"
        Me.txtMaxProfitPercentagePerDay.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxProfitPercentagePerDay.TabIndex = 6
        '
        'lblMaxProfitPercentagePerDay
        '
        Me.lblMaxProfitPercentagePerDay.AutoSize = True
        Me.lblMaxProfitPercentagePerDay.Location = New System.Drawing.Point(8, 241)
        Me.lblMaxProfitPercentagePerDay.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMaxProfitPercentagePerDay.Name = "lblMaxProfitPercentagePerDay"
        Me.lblMaxProfitPercentagePerDay.Size = New System.Drawing.Size(141, 17)
        Me.lblMaxProfitPercentagePerDay.TabIndex = 27
        Me.lblMaxProfitPercentagePerDay.Text = "Max Profit % Per Day"
        '
        'txtMaxLossPercentagePerDay
        '
        Me.txtMaxLossPercentagePerDay.Location = New System.Drawing.Point(174, 202)
        Me.txtMaxLossPercentagePerDay.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMaxLossPercentagePerDay.Name = "txtMaxLossPercentagePerDay"
        Me.txtMaxLossPercentagePerDay.Size = New System.Drawing.Size(255, 22)
        Me.txtMaxLossPercentagePerDay.TabIndex = 5
        '
        'lblMaxLossPercentagePerDay
        '
        Me.lblMaxLossPercentagePerDay.AutoSize = True
        Me.lblMaxLossPercentagePerDay.Location = New System.Drawing.Point(8, 206)
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
        'txtMinCandleRangePercentage
        '
        Me.txtMinCandleRangePercentage.Location = New System.Drawing.Point(174, 313)
        Me.txtMinCandleRangePercentage.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMinCandleRangePercentage.Name = "txtMinCandleRangePercentage"
        Me.txtMinCandleRangePercentage.Size = New System.Drawing.Size(256, 22)
        Me.txtMinCandleRangePercentage.TabIndex = 8
        '
        'lblMinCandleRangePercentage
        '
        Me.lblMinCandleRangePercentage.AutoSize = True
        Me.lblMinCandleRangePercentage.Location = New System.Drawing.Point(8, 316)
        Me.lblMinCandleRangePercentage.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMinCandleRangePercentage.Name = "lblMinCandleRangePercentage"
        Me.lblMinCandleRangePercentage.Size = New System.Drawing.Size(140, 17)
        Me.lblMinCandleRangePercentage.TabIndex = 11
        Me.lblMinCandleRangePercentage.Text = "Min Candle Range %"
        '
        'btnBrowse
        '
        Me.btnBrowse.Font = New System.Drawing.Font("Microsoft Sans Serif", 7.8!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnBrowse.Location = New System.Drawing.Point(404, 387)
        Me.btnBrowse.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.btnBrowse.Name = "btnBrowse"
        Me.btnBrowse.Size = New System.Drawing.Size(40, 23)
        Me.btnBrowse.TabIndex = 10
        Me.btnBrowse.Text = "..."
        Me.btnBrowse.UseVisualStyleBackColor = True
        '
        'txtInstrumentDetalis
        '
        Me.txtInstrumentDetalis.Location = New System.Drawing.Point(174, 388)
        Me.txtInstrumentDetalis.Margin = New System.Windows.Forms.Padding(4)
        Me.txtInstrumentDetalis.Name = "txtInstrumentDetalis"
        Me.txtInstrumentDetalis.ReadOnly = True
        Me.txtInstrumentDetalis.Size = New System.Drawing.Size(223, 22)
        Me.txtInstrumentDetalis.TabIndex = 15
        '
        'lblInstrumentDetails
        '
        Me.lblInstrumentDetails.AutoSize = True
        Me.lblInstrumentDetails.Location = New System.Drawing.Point(8, 391)
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
        'txtTargetMultiplier
        '
        Me.txtTargetMultiplier.Location = New System.Drawing.Point(175, 165)
        Me.txtTargetMultiplier.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTargetMultiplier.Name = "txtTargetMultiplier"
        Me.txtTargetMultiplier.Size = New System.Drawing.Size(256, 22)
        Me.txtTargetMultiplier.TabIndex = 4
        '
        'txtCapitalProtectionPercentage
        '
        Me.txtCapitalProtectionPercentage.Location = New System.Drawing.Point(174, 351)
        Me.txtCapitalProtectionPercentage.Margin = New System.Windows.Forms.Padding(4)
        Me.txtCapitalProtectionPercentage.Name = "txtCapitalProtectionPercentage"
        Me.txtCapitalProtectionPercentage.Size = New System.Drawing.Size(256, 22)
        Me.txtCapitalProtectionPercentage.TabIndex = 9
        '
        'txtCandleWickSizePercentage
        '
        Me.txtCandleWickSizePercentage.Location = New System.Drawing.Point(174, 274)
        Me.txtCandleWickSizePercentage.Margin = New System.Windows.Forms.Padding(4)
        Me.txtCandleWickSizePercentage.Name = "txtCandleWickSizePercentage"
        Me.txtCandleWickSizePercentage.Size = New System.Drawing.Size(255, 22)
        Me.txtCandleWickSizePercentage.TabIndex = 7
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
        'lblTargetMultiplier
        '
        Me.lblTargetMultiplier.AutoSize = True
        Me.lblTargetMultiplier.Location = New System.Drawing.Point(9, 169)
        Me.lblTargetMultiplier.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTargetMultiplier.Name = "lblTargetMultiplier"
        Me.lblTargetMultiplier.Size = New System.Drawing.Size(110, 17)
        Me.lblTargetMultiplier.TabIndex = 2
        Me.lblTargetMultiplier.Text = "Target Multiplier"
        '
        'lblCapitalProtectionPercentage
        '
        Me.lblCapitalProtectionPercentage.AutoSize = True
        Me.lblCapitalProtectionPercentage.Location = New System.Drawing.Point(7, 354)
        Me.lblCapitalProtectionPercentage.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblCapitalProtectionPercentage.Name = "lblCapitalProtectionPercentage"
        Me.lblCapitalProtectionPercentage.Size = New System.Drawing.Size(135, 17)
        Me.lblCapitalProtectionPercentage.TabIndex = 1
        Me.lblCapitalProtectionPercentage.Text = "Capital Protection %"
        '
        'lblCandleWickSizePercentage
        '
        Me.lblCandleWickSizePercentage.AutoSize = True
        Me.lblCandleWickSizePercentage.Location = New System.Drawing.Point(8, 277)
        Me.lblCandleWickSizePercentage.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblCandleWickSizePercentage.Name = "lblCandleWickSizePercentage"
        Me.lblCandleWickSizePercentage.Size = New System.Drawing.Size(133, 17)
        Me.lblCandleWickSizePercentage.TabIndex = 0
        Me.lblCandleWickSizePercentage.Text = "Candle Wick Size %"
        '
        'opnFileSettings
        '
        '
        'frmMomentumReversalSettings
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(575, 439)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.btnSaveMomentumReversalSettings)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Margin = New System.Windows.Forms.Padding(4)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "frmMomentumReversalSettings"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Momentum Reversal  - Settings"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents btnSaveMomentumReversalSettings As Button
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents txtSignalTimeFrame As TextBox
    Friend WithEvents txtTargetMultiplier As TextBox
    Friend WithEvents txtCapitalProtectionPercentage As TextBox
    Friend WithEvents txtCandleWickSizePercentage As TextBox
    Friend WithEvents lblSignalTimeFrame As Label
    Friend WithEvents lblTargetMultiplier As Label
    Friend WithEvents lblCapitalProtectionPercentage As Label
    Friend WithEvents lblCandleWickSizePercentage As Label
    Friend WithEvents ImageList1 As ImageList
    Friend WithEvents btnBrowse As Button
    Friend WithEvents txtInstrumentDetalis As TextBox
    Friend WithEvents lblInstrumentDetails As Label
    Friend WithEvents opnFileSettings As OpenFileDialog
    Friend WithEvents txtMinCandleRangePercentage As TextBox
    Friend WithEvents lblMinCandleRangePercentage As Label
    Friend WithEvents lblEODExitTime As Label
    Friend WithEvents lblLastTradeEntryTime As Label
    Friend WithEvents lblTradeStartTime As Label
    Friend WithEvents dtpckrTradeStartTime As DateTimePicker
    Friend WithEvents dtpckrEODExitTime As DateTimePicker
    Friend WithEvents dtpckrLastTradeEntryTime As DateTimePicker
    Friend WithEvents txtMaxProfitPercentagePerDay As TextBox
    Friend WithEvents lblMaxProfitPercentagePerDay As Label
    Friend WithEvents txtMaxLossPercentagePerDay As TextBox
    Friend WithEvents lblMaxLossPercentagePerDay As Label
End Class
