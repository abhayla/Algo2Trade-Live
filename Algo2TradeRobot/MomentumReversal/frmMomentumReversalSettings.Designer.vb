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
        Me.opnFileSettings = New System.Windows.Forms.OpenFileDialog()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.txtNumberOfTradePerStock = New System.Windows.Forms.TextBox()
        Me.lblNumberOfTradePerStock = New System.Windows.Forms.Label()
        Me.txtCostToCostMovement = New System.Windows.Forms.TextBox()
        Me.lblCostToCostMvmnt = New System.Windows.Forms.Label()
        Me.txtMinTarget = New System.Windows.Forms.TextBox()
        Me.lblMinTarget = New System.Windows.Forms.Label()
        Me.txtMinStoploss = New System.Windows.Forms.TextBox()
        Me.lblMinStoploss = New System.Windows.Forms.Label()
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
        Me.lblATRPeriod = New System.Windows.Forms.Label()
        Me.txtATRPeriod = New System.Windows.Forms.TextBox()
        Me.grpATR = New System.Windows.Forms.GroupBox()
        Me.GroupBox1.SuspendLayout()
        Me.grpATR.SuspendLayout()
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
        Me.btnSaveMomentumReversalSettings.TabIndex = 0
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
        'opnFileSettings
        '
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.txtNumberOfTradePerStock)
        Me.GroupBox1.Controls.Add(Me.lblNumberOfTradePerStock)
        Me.GroupBox1.Controls.Add(Me.txtCostToCostMovement)
        Me.GroupBox1.Controls.Add(Me.lblCostToCostMvmnt)
        Me.GroupBox1.Controls.Add(Me.txtMinTarget)
        Me.GroupBox1.Controls.Add(Me.lblMinTarget)
        Me.GroupBox1.Controls.Add(Me.txtMinStoploss)
        Me.GroupBox1.Controls.Add(Me.lblMinStoploss)
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
        Me.GroupBox1.Location = New System.Drawing.Point(2, 3)
        Me.GroupBox1.Margin = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Padding = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Size = New System.Drawing.Size(451, 321)
        Me.GroupBox1.TabIndex = 22
        Me.GroupBox1.TabStop = False
        '
        'txtNumberOfTradePerStock
        '
        Me.txtNumberOfTradePerStock.Location = New System.Drawing.Point(189, 152)
        Me.txtNumberOfTradePerStock.Margin = New System.Windows.Forms.Padding(4)
        Me.txtNumberOfTradePerStock.Name = "txtNumberOfTradePerStock"
        Me.txtNumberOfTradePerStock.Size = New System.Drawing.Size(241, 22)
        Me.txtNumberOfTradePerStock.TabIndex = 5
        Me.txtNumberOfTradePerStock.Tag = "Number Of Trade Per Stock"
        '
        'lblNumberOfTradePerStock
        '
        Me.lblNumberOfTradePerStock.AutoSize = True
        Me.lblNumberOfTradePerStock.Location = New System.Drawing.Point(9, 155)
        Me.lblNumberOfTradePerStock.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblNumberOfTradePerStock.Name = "lblNumberOfTradePerStock"
        Me.lblNumberOfTradePerStock.Size = New System.Drawing.Size(152, 17)
        Me.lblNumberOfTradePerStock.TabIndex = 43
        Me.lblNumberOfTradePerStock.Text = "No Of Trade Per Stock"
        '
        'txtCostToCostMovement
        '
        Me.txtCostToCostMovement.Location = New System.Drawing.Point(189, 249)
        Me.txtCostToCostMovement.Margin = New System.Windows.Forms.Padding(4)
        Me.txtCostToCostMovement.Name = "txtCostToCostMovement"
        Me.txtCostToCostMovement.Size = New System.Drawing.Size(241, 22)
        Me.txtCostToCostMovement.TabIndex = 8
        Me.txtCostToCostMovement.Tag = "Cost To Cost Movement"
        '
        'lblCostToCostMvmnt
        '
        Me.lblCostToCostMvmnt.AutoSize = True
        Me.lblCostToCostMvmnt.Location = New System.Drawing.Point(9, 254)
        Me.lblCostToCostMvmnt.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblCostToCostMvmnt.Name = "lblCostToCostMvmnt"
        Me.lblCostToCostMvmnt.Size = New System.Drawing.Size(158, 17)
        Me.lblCostToCostMvmnt.TabIndex = 41
        Me.lblCostToCostMvmnt.Text = "Cost To Cost Movement"
        '
        'txtMinTarget
        '
        Me.txtMinTarget.Location = New System.Drawing.Point(189, 215)
        Me.txtMinTarget.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMinTarget.Name = "txtMinTarget"
        Me.txtMinTarget.Size = New System.Drawing.Size(241, 22)
        Me.txtMinTarget.TabIndex = 7
        Me.txtMinTarget.Tag = "Min Target %"
        '
        'lblMinTarget
        '
        Me.lblMinTarget.AutoSize = True
        Me.lblMinTarget.Location = New System.Drawing.Point(9, 220)
        Me.lblMinTarget.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMinTarget.Name = "lblMinTarget"
        Me.lblMinTarget.Size = New System.Drawing.Size(92, 17)
        Me.lblMinTarget.TabIndex = 39
        Me.lblMinTarget.Text = "Min Target %"
        '
        'txtMinStoploss
        '
        Me.txtMinStoploss.Location = New System.Drawing.Point(189, 184)
        Me.txtMinStoploss.Margin = New System.Windows.Forms.Padding(4)
        Me.txtMinStoploss.Name = "txtMinStoploss"
        Me.txtMinStoploss.Size = New System.Drawing.Size(241, 22)
        Me.txtMinStoploss.TabIndex = 6
        Me.txtMinStoploss.Tag = "Min Stoploss %"
        '
        'lblMinStoploss
        '
        Me.lblMinStoploss.AutoSize = True
        Me.lblMinStoploss.Location = New System.Drawing.Point(9, 189)
        Me.lblMinStoploss.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMinStoploss.Name = "lblMinStoploss"
        Me.lblMinStoploss.Size = New System.Drawing.Size(104, 17)
        Me.lblMinStoploss.TabIndex = 37
        Me.lblMinStoploss.Text = "Min Stoploss %"
        '
        'dtpckrEODExitTime
        '
        Me.dtpckrEODExitTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrEODExitTime.Location = New System.Drawing.Point(189, 119)
        Me.dtpckrEODExitTime.Name = "dtpckrEODExitTime"
        Me.dtpckrEODExitTime.ShowUpDown = True
        Me.dtpckrEODExitTime.Size = New System.Drawing.Size(241, 22)
        Me.dtpckrEODExitTime.TabIndex = 4
        Me.dtpckrEODExitTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'dtpckrLastTradeEntryTime
        '
        Me.dtpckrLastTradeEntryTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrLastTradeEntryTime.Location = New System.Drawing.Point(189, 84)
        Me.dtpckrLastTradeEntryTime.Name = "dtpckrLastTradeEntryTime"
        Me.dtpckrLastTradeEntryTime.ShowUpDown = True
        Me.dtpckrLastTradeEntryTime.Size = New System.Drawing.Size(242, 22)
        Me.dtpckrLastTradeEntryTime.TabIndex = 3
        Me.dtpckrLastTradeEntryTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'dtpckrTradeStartTime
        '
        Me.dtpckrTradeStartTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrTradeStartTime.Location = New System.Drawing.Point(189, 49)
        Me.dtpckrTradeStartTime.Name = "dtpckrTradeStartTime"
        Me.dtpckrTradeStartTime.ShowUpDown = True
        Me.dtpckrTradeStartTime.Size = New System.Drawing.Size(241, 22)
        Me.dtpckrTradeStartTime.TabIndex = 2
        Me.dtpckrTradeStartTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'lblEODExitTime
        '
        Me.lblEODExitTime.AutoSize = True
        Me.lblEODExitTime.Location = New System.Drawing.Point(9, 120)
        Me.lblEODExitTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblEODExitTime.Name = "lblEODExitTime"
        Me.lblEODExitTime.Size = New System.Drawing.Size(99, 17)
        Me.lblEODExitTime.TabIndex = 23
        Me.lblEODExitTime.Text = "EOD Exit Time"
        '
        'lblLastTradeEntryTime
        '
        Me.lblLastTradeEntryTime.AutoSize = True
        Me.lblLastTradeEntryTime.Location = New System.Drawing.Point(9, 85)
        Me.lblLastTradeEntryTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblLastTradeEntryTime.Name = "lblLastTradeEntryTime"
        Me.lblLastTradeEntryTime.Size = New System.Drawing.Size(149, 17)
        Me.lblLastTradeEntryTime.TabIndex = 21
        Me.lblLastTradeEntryTime.Text = "Last Trade Entry Time"
        '
        'lblTradeStartTime
        '
        Me.lblTradeStartTime.AutoSize = True
        Me.lblTradeStartTime.Location = New System.Drawing.Point(9, 51)
        Me.lblTradeStartTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTradeStartTime.Name = "lblTradeStartTime"
        Me.lblTradeStartTime.Size = New System.Drawing.Size(115, 17)
        Me.lblTradeStartTime.TabIndex = 19
        Me.lblTradeStartTime.Text = "Trade Start Time"
        '
        'btnBrowse
        '
        Me.btnBrowse.Font = New System.Drawing.Font("Microsoft Sans Serif", 7.8!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnBrowse.Location = New System.Drawing.Point(404, 281)
        Me.btnBrowse.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.btnBrowse.Name = "btnBrowse"
        Me.btnBrowse.Size = New System.Drawing.Size(40, 23)
        Me.btnBrowse.TabIndex = 9
        Me.btnBrowse.Text = "..."
        Me.btnBrowse.UseVisualStyleBackColor = True
        '
        'txtInstrumentDetalis
        '
        Me.txtInstrumentDetalis.Location = New System.Drawing.Point(189, 282)
        Me.txtInstrumentDetalis.Margin = New System.Windows.Forms.Padding(4)
        Me.txtInstrumentDetalis.Name = "txtInstrumentDetalis"
        Me.txtInstrumentDetalis.ReadOnly = True
        Me.txtInstrumentDetalis.Size = New System.Drawing.Size(208, 22)
        Me.txtInstrumentDetalis.TabIndex = 9
        '
        'lblInstrumentDetails
        '
        Me.lblInstrumentDetails.AutoSize = True
        Me.lblInstrumentDetails.Location = New System.Drawing.Point(8, 285)
        Me.lblInstrumentDetails.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblInstrumentDetails.Name = "lblInstrumentDetails"
        Me.lblInstrumentDetails.Size = New System.Drawing.Size(121, 17)
        Me.lblInstrumentDetails.TabIndex = 8
        Me.lblInstrumentDetails.Text = "Instrument Details"
        '
        'txtSignalTimeFrame
        '
        Me.txtSignalTimeFrame.Location = New System.Drawing.Point(189, 15)
        Me.txtSignalTimeFrame.Margin = New System.Windows.Forms.Padding(4)
        Me.txtSignalTimeFrame.Name = "txtSignalTimeFrame"
        Me.txtSignalTimeFrame.Size = New System.Drawing.Size(241, 22)
        Me.txtSignalTimeFrame.TabIndex = 1
        Me.txtSignalTimeFrame.Tag = "Signal Timeframe"
        '
        'lblSignalTimeFrame
        '
        Me.lblSignalTimeFrame.AutoSize = True
        Me.lblSignalTimeFrame.Location = New System.Drawing.Point(9, 18)
        Me.lblSignalTimeFrame.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblSignalTimeFrame.Name = "lblSignalTimeFrame"
        Me.lblSignalTimeFrame.Size = New System.Drawing.Size(158, 17)
        Me.lblSignalTimeFrame.TabIndex = 3
        Me.lblSignalTimeFrame.Tag = ""
        Me.lblSignalTimeFrame.Text = "Signal Time Frame(min)"
        '
        'lblATRPeriod
        '
        Me.lblATRPeriod.AutoSize = True
        Me.lblATRPeriod.Location = New System.Drawing.Point(9, 26)
        Me.lblATRPeriod.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblATRPeriod.Name = "lblATRPeriod"
        Me.lblATRPeriod.Size = New System.Drawing.Size(81, 17)
        Me.lblATRPeriod.TabIndex = 35
        Me.lblATRPeriod.Text = "ATR Period"
        '
        'txtATRPeriod
        '
        Me.txtATRPeriod.Location = New System.Drawing.Point(189, 21)
        Me.txtATRPeriod.Margin = New System.Windows.Forms.Padding(4)
        Me.txtATRPeriod.Name = "txtATRPeriod"
        Me.txtATRPeriod.Size = New System.Drawing.Size(241, 22)
        Me.txtATRPeriod.TabIndex = 10
        Me.txtATRPeriod.Tag = "ATR Period"
        '
        'grpATR
        '
        Me.grpATR.Controls.Add(Me.txtATRPeriod)
        Me.grpATR.Controls.Add(Me.lblATRPeriod)
        Me.grpATR.Location = New System.Drawing.Point(2, 325)
        Me.grpATR.Name = "grpATR"
        Me.grpATR.Size = New System.Drawing.Size(451, 59)
        Me.grpATR.TabIndex = 23
        Me.grpATR.TabStop = False
        Me.grpATR.Text = "ATR Settings"
        '
        'frmMomentumReversalSettings
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(576, 389)
        Me.Controls.Add(Me.grpATR)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.btnSaveMomentumReversalSettings)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Margin = New System.Windows.Forms.Padding(4)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "frmMomentumReversalSettings"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Settings"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.grpATR.ResumeLayout(False)
        Me.grpATR.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents btnSaveMomentumReversalSettings As Button
    Friend WithEvents ImageList1 As ImageList
    Friend WithEvents opnFileSettings As OpenFileDialog
    Friend WithEvents GroupBox1 As GroupBox
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
    Friend WithEvents lblATRPeriod As Label
    Friend WithEvents txtATRPeriod As TextBox
    Friend WithEvents grpATR As GroupBox
    Friend WithEvents txtCostToCostMovement As TextBox
    Friend WithEvents lblCostToCostMvmnt As Label
    Friend WithEvents txtMinTarget As TextBox
    Friend WithEvents lblMinTarget As Label
    Friend WithEvents txtMinStoploss As TextBox
    Friend WithEvents lblMinStoploss As Label
    Friend WithEvents txtNumberOfTradePerStock As TextBox
    Friend WithEvents lblNumberOfTradePerStock As Label
End Class
