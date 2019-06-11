<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmEMACrossoverSettings
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmEMACrossoverSettings))
        Me.ImageList1 = New System.Windows.Forms.ImageList(Me.components)
        Me.opnFileSettings = New System.Windows.Forms.OpenFileDialog()
        Me.btnSaveEMACrossoverSettings = New System.Windows.Forms.Button()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.txtSlowEMAPeriod = New System.Windows.Forms.TextBox()
        Me.lblSlowEMAPeriod = New System.Windows.Forms.Label()
        Me.txtFastEMAPeriod = New System.Windows.Forms.TextBox()
        Me.lblFastEMAPeriod = New System.Windows.Forms.Label()
        Me.lblSignalTimeFrame = New System.Windows.Forms.Label()
        Me.txtSignalTimeFrame = New System.Windows.Forms.TextBox()
        Me.lblInstrumentDetails = New System.Windows.Forms.Label()
        Me.txtInstrumentDetalis = New System.Windows.Forms.TextBox()
        Me.btnBrowse = New System.Windows.Forms.Button()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.txtTradeEntryDelay = New System.Windows.Forms.TextBox()
        Me.lblTradeEntryDelay = New System.Windows.Forms.Label()
        Me.GroupBox2.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
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
        'btnSaveEMACrossoverSettings
        '
        Me.btnSaveEMACrossoverSettings.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnSaveEMACrossoverSettings.ImageKey = "save-icon-36533.png"
        Me.btnSaveEMACrossoverSettings.ImageList = Me.ImageList1
        Me.btnSaveEMACrossoverSettings.Location = New System.Drawing.Point(464, 13)
        Me.btnSaveEMACrossoverSettings.Margin = New System.Windows.Forms.Padding(4)
        Me.btnSaveEMACrossoverSettings.Name = "btnSaveEMACrossoverSettings"
        Me.btnSaveEMACrossoverSettings.Size = New System.Drawing.Size(112, 58)
        Me.btnSaveEMACrossoverSettings.TabIndex = 20
        Me.btnSaveEMACrossoverSettings.Text = "&Save"
        Me.btnSaveEMACrossoverSettings.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnSaveEMACrossoverSettings.UseVisualStyleBackColor = True
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.txtSlowEMAPeriod)
        Me.GroupBox2.Controls.Add(Me.lblSlowEMAPeriod)
        Me.GroupBox2.Controls.Add(Me.txtFastEMAPeriod)
        Me.GroupBox2.Controls.Add(Me.lblFastEMAPeriod)
        Me.GroupBox2.Location = New System.Drawing.Point(5, 137)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(451, 93)
        Me.GroupBox2.TabIndex = 22
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Indicator Settings"
        '
        'txtSlowEMAPeriod
        '
        Me.txtSlowEMAPeriod.Location = New System.Drawing.Point(175, 56)
        Me.txtSlowEMAPeriod.Margin = New System.Windows.Forms.Padding(4)
        Me.txtSlowEMAPeriod.Name = "txtSlowEMAPeriod"
        Me.txtSlowEMAPeriod.Size = New System.Drawing.Size(255, 22)
        Me.txtSlowEMAPeriod.TabIndex = 32
        Me.txtSlowEMAPeriod.Tag = "Slow EMA Period"
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
        Me.txtFastEMAPeriod.Location = New System.Drawing.Point(174, 25)
        Me.txtFastEMAPeriod.Margin = New System.Windows.Forms.Padding(4)
        Me.txtFastEMAPeriod.Name = "txtFastEMAPeriod"
        Me.txtFastEMAPeriod.Size = New System.Drawing.Size(256, 22)
        Me.txtFastEMAPeriod.TabIndex = 30
        Me.txtFastEMAPeriod.Tag = "Fast EMA Period"
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
        'txtSignalTimeFrame
        '
        Me.txtSignalTimeFrame.Location = New System.Drawing.Point(175, 22)
        Me.txtSignalTimeFrame.Margin = New System.Windows.Forms.Padding(4)
        Me.txtSignalTimeFrame.Name = "txtSignalTimeFrame"
        Me.txtSignalTimeFrame.Size = New System.Drawing.Size(255, 22)
        Me.txtSignalTimeFrame.TabIndex = 0
        Me.txtSignalTimeFrame.Tag = "Signal Time Frame"
        '
        'lblInstrumentDetails
        '
        Me.lblInstrumentDetails.AutoSize = True
        Me.lblInstrumentDetails.Location = New System.Drawing.Point(9, 97)
        Me.lblInstrumentDetails.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblInstrumentDetails.Name = "lblInstrumentDetails"
        Me.lblInstrumentDetails.Size = New System.Drawing.Size(121, 17)
        Me.lblInstrumentDetails.TabIndex = 8
        Me.lblInstrumentDetails.Text = "Instrument Details"
        '
        'txtInstrumentDetalis
        '
        Me.txtInstrumentDetalis.Location = New System.Drawing.Point(174, 94)
        Me.txtInstrumentDetalis.Margin = New System.Windows.Forms.Padding(4)
        Me.txtInstrumentDetalis.Name = "txtInstrumentDetalis"
        Me.txtInstrumentDetalis.ReadOnly = True
        Me.txtInstrumentDetalis.Size = New System.Drawing.Size(223, 22)
        Me.txtInstrumentDetalis.TabIndex = 15
        '
        'btnBrowse
        '
        Me.btnBrowse.Font = New System.Drawing.Font("Microsoft Sans Serif", 7.8!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnBrowse.Location = New System.Drawing.Point(404, 93)
        Me.btnBrowse.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.btnBrowse.Name = "btnBrowse"
        Me.btnBrowse.Size = New System.Drawing.Size(40, 23)
        Me.btnBrowse.TabIndex = 8
        Me.btnBrowse.Text = "..."
        Me.btnBrowse.UseVisualStyleBackColor = True
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.txtTradeEntryDelay)
        Me.GroupBox1.Controls.Add(Me.lblTradeEntryDelay)
        Me.GroupBox1.Controls.Add(Me.btnBrowse)
        Me.GroupBox1.Controls.Add(Me.txtInstrumentDetalis)
        Me.GroupBox1.Controls.Add(Me.lblInstrumentDetails)
        Me.GroupBox1.Controls.Add(Me.txtSignalTimeFrame)
        Me.GroupBox1.Controls.Add(Me.lblSignalTimeFrame)
        Me.GroupBox1.Location = New System.Drawing.Point(5, 6)
        Me.GroupBox1.Margin = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Padding = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Size = New System.Drawing.Size(451, 124)
        Me.GroupBox1.TabIndex = 21
        Me.GroupBox1.TabStop = False
        '
        'txtTradeEntryDelay
        '
        Me.txtTradeEntryDelay.Location = New System.Drawing.Point(175, 57)
        Me.txtTradeEntryDelay.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTradeEntryDelay.Name = "txtTradeEntryDelay"
        Me.txtTradeEntryDelay.Size = New System.Drawing.Size(255, 22)
        Me.txtTradeEntryDelay.TabIndex = 16
        Me.txtTradeEntryDelay.Tag = "Trade Entry Delay"
        '
        'lblTradeEntryDelay
        '
        Me.lblTradeEntryDelay.AutoSize = True
        Me.lblTradeEntryDelay.Location = New System.Drawing.Point(9, 60)
        Me.lblTradeEntryDelay.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTradeEntryDelay.Name = "lblTradeEntryDelay"
        Me.lblTradeEntryDelay.Size = New System.Drawing.Size(155, 17)
        Me.lblTradeEntryDelay.TabIndex = 17
        Me.lblTradeEntryDelay.Text = "Trade Entry Delay(min)"
        '
        'frmEMACrossoverSettings
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(583, 237)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.btnSaveEMACrossoverSettings)
        Me.Controls.Add(Me.GroupBox1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "frmEMACrossoverSettings"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "EMA Crossover Strategy - Settings"
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents ImageList1 As ImageList
    Friend WithEvents opnFileSettings As OpenFileDialog
    Friend WithEvents btnSaveEMACrossoverSettings As Button
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents txtSlowEMAPeriod As TextBox
    Friend WithEvents lblSlowEMAPeriod As Label
    Friend WithEvents txtFastEMAPeriod As TextBox
    Friend WithEvents lblFastEMAPeriod As Label
    Friend WithEvents lblSignalTimeFrame As Label
    Friend WithEvents txtSignalTimeFrame As TextBox
    Friend WithEvents lblInstrumentDetails As Label
    Friend WithEvents txtInstrumentDetalis As TextBox
    Friend WithEvents btnBrowse As Button
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents txtTradeEntryDelay As TextBox
    Friend WithEvents lblTradeEntryDelay As Label
End Class
