<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class frmAdvancedOptions
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmAdvancedOptions))
        Me.tabMain = New System.Windows.Forms.TabControl()
        Me.tabDelaySettings = New System.Windows.Forms.TabPage()
        Me.dtpckrForceRestartTime = New System.Windows.Forms.DateTimePicker()
        Me.lblForceRestartTime = New System.Windows.Forms.Label()
        Me.txtBackToBackOrderCoolOffDelay = New System.Windows.Forms.TextBox()
        Me.lblBackToBackOrderCoolOffDelay = New System.Windows.Forms.Label()
        Me.txtGetInformationDelay = New System.Windows.Forms.TextBox()
        Me.lblGetInformationDelay = New System.Windows.Forms.Label()
        Me.tabExchangeDetailsSettings = New System.Windows.Forms.TabPage()
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
        Me.dtpckrCDSExchangeEndTime = New System.Windows.Forms.DateTimePicker()
        Me.lblCDSExchangeEndTime = New System.Windows.Forms.Label()
        Me.dtpckrCDSExchangeStartTime = New System.Windows.Forms.DateTimePicker()
        Me.lblCDSExchangeStartTime = New System.Windows.Forms.Label()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.dtpckrMCXExchangeEndTime = New System.Windows.Forms.DateTimePicker()
        Me.lblMCXExchangeEndTime = New System.Windows.Forms.Label()
        Me.dtpckrMCXExchangeStartTime = New System.Windows.Forms.DateTimePicker()
        Me.lblMCXExchangeStartTime = New System.Windows.Forms.Label()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.dtpckrNSEExchangeEndTime = New System.Windows.Forms.DateTimePicker()
        Me.lblNSEExcahngeEndTime = New System.Windows.Forms.Label()
        Me.dtpckrNSEExchangeStartTime = New System.Windows.Forms.DateTimePicker()
        Me.lblNSEExchangeStartTime = New System.Windows.Forms.Label()
        Me.ImageList1 = New System.Windows.Forms.ImageList(Me.components)
        Me.btnSaveDelaySettings = New System.Windows.Forms.Button()
        Me.tabRemarks = New System.Windows.Forms.TabPage()
        Me.txtRemarks = New System.Windows.Forms.TextBox()
        Me.lblRemarks = New System.Windows.Forms.Label()
        Me.tabMain.SuspendLayout()
        Me.tabDelaySettings.SuspendLayout()
        Me.tabExchangeDetailsSettings.SuspendLayout()
        Me.GroupBox3.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.tabRemarks.SuspendLayout()
        Me.SuspendLayout()
        '
        'tabMain
        '
        Me.tabMain.Controls.Add(Me.tabDelaySettings)
        Me.tabMain.Controls.Add(Me.tabExchangeDetailsSettings)
        Me.tabMain.Controls.Add(Me.tabRemarks)
        Me.tabMain.Location = New System.Drawing.Point(0, 0)
        Me.tabMain.Name = "tabMain"
        Me.tabMain.SelectedIndex = 0
        Me.tabMain.Size = New System.Drawing.Size(424, 311)
        Me.tabMain.TabIndex = 0
        '
        'tabDelaySettings
        '
        Me.tabDelaySettings.Controls.Add(Me.dtpckrForceRestartTime)
        Me.tabDelaySettings.Controls.Add(Me.lblForceRestartTime)
        Me.tabDelaySettings.Controls.Add(Me.txtBackToBackOrderCoolOffDelay)
        Me.tabDelaySettings.Controls.Add(Me.lblBackToBackOrderCoolOffDelay)
        Me.tabDelaySettings.Controls.Add(Me.txtGetInformationDelay)
        Me.tabDelaySettings.Controls.Add(Me.lblGetInformationDelay)
        Me.tabDelaySettings.Location = New System.Drawing.Point(4, 25)
        Me.tabDelaySettings.Name = "tabDelaySettings"
        Me.tabDelaySettings.Padding = New System.Windows.Forms.Padding(3)
        Me.tabDelaySettings.Size = New System.Drawing.Size(416, 282)
        Me.tabDelaySettings.TabIndex = 0
        Me.tabDelaySettings.Text = "Delay"
        Me.tabDelaySettings.UseVisualStyleBackColor = True
        '
        'dtpckrForceRestartTime
        '
        Me.dtpckrForceRestartTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrForceRestartTime.Location = New System.Drawing.Point(268, 96)
        Me.dtpckrForceRestartTime.Name = "dtpckrForceRestartTime"
        Me.dtpckrForceRestartTime.ShowUpDown = True
        Me.dtpckrForceRestartTime.Size = New System.Drawing.Size(134, 22)
        Me.dtpckrForceRestartTime.TabIndex = 3
        Me.dtpckrForceRestartTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'lblForceRestartTime
        '
        Me.lblForceRestartTime.AutoSize = True
        Me.lblForceRestartTime.Location = New System.Drawing.Point(6, 98)
        Me.lblForceRestartTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblForceRestartTime.Name = "lblForceRestartTime"
        Me.lblForceRestartTime.Size = New System.Drawing.Size(129, 17)
        Me.lblForceRestartTime.TabIndex = 21
        Me.lblForceRestartTime.Text = "Force Restart Time"
        '
        'txtBackToBackOrderCoolOffDelay
        '
        Me.txtBackToBackOrderCoolOffDelay.Location = New System.Drawing.Point(268, 57)
        Me.txtBackToBackOrderCoolOffDelay.Margin = New System.Windows.Forms.Padding(4)
        Me.txtBackToBackOrderCoolOffDelay.Name = "txtBackToBackOrderCoolOffDelay"
        Me.txtBackToBackOrderCoolOffDelay.Size = New System.Drawing.Size(134, 22)
        Me.txtBackToBackOrderCoolOffDelay.TabIndex = 2
        '
        'lblBackToBackOrderCoolOffDelay
        '
        Me.lblBackToBackOrderCoolOffDelay.AutoSize = True
        Me.lblBackToBackOrderCoolOffDelay.Location = New System.Drawing.Point(6, 58)
        Me.lblBackToBackOrderCoolOffDelay.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblBackToBackOrderCoolOffDelay.Name = "lblBackToBackOrderCoolOffDelay"
        Me.lblBackToBackOrderCoolOffDelay.Size = New System.Drawing.Size(255, 17)
        Me.lblBackToBackOrderCoolOffDelay.TabIndex = 12
        Me.lblBackToBackOrderCoolOffDelay.Text = "BackToBack Order CoolOff Delay (sec)"
        '
        'txtGetInformationDelay
        '
        Me.txtGetInformationDelay.Location = New System.Drawing.Point(268, 17)
        Me.txtGetInformationDelay.Margin = New System.Windows.Forms.Padding(4)
        Me.txtGetInformationDelay.Name = "txtGetInformationDelay"
        Me.txtGetInformationDelay.Size = New System.Drawing.Size(134, 22)
        Me.txtGetInformationDelay.TabIndex = 1
        '
        'lblGetInformationDelay
        '
        Me.lblGetInformationDelay.AutoSize = True
        Me.lblGetInformationDelay.Location = New System.Drawing.Point(6, 18)
        Me.lblGetInformationDelay.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblGetInformationDelay.Name = "lblGetInformationDelay"
        Me.lblGetInformationDelay.Size = New System.Drawing.Size(181, 17)
        Me.lblGetInformationDelay.TabIndex = 10
        Me.lblGetInformationDelay.Text = "Get Information Delay (sec)"
        '
        'tabExchangeDetailsSettings
        '
        Me.tabExchangeDetailsSettings.Controls.Add(Me.GroupBox3)
        Me.tabExchangeDetailsSettings.Controls.Add(Me.GroupBox2)
        Me.tabExchangeDetailsSettings.Controls.Add(Me.GroupBox1)
        Me.tabExchangeDetailsSettings.Location = New System.Drawing.Point(4, 25)
        Me.tabExchangeDetailsSettings.Name = "tabExchangeDetailsSettings"
        Me.tabExchangeDetailsSettings.Size = New System.Drawing.Size(416, 282)
        Me.tabExchangeDetailsSettings.TabIndex = 1
        Me.tabExchangeDetailsSettings.Text = "Exchange Details"
        Me.tabExchangeDetailsSettings.UseVisualStyleBackColor = True
        '
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.dtpckrCDSExchangeEndTime)
        Me.GroupBox3.Controls.Add(Me.lblCDSExchangeEndTime)
        Me.GroupBox3.Controls.Add(Me.dtpckrCDSExchangeStartTime)
        Me.GroupBox3.Controls.Add(Me.lblCDSExchangeStartTime)
        Me.GroupBox3.Location = New System.Drawing.Point(9, 185)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(404, 85)
        Me.GroupBox3.TabIndex = 31
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "CDS"
        '
        'dtpckrCDSExchangeEndTime
        '
        Me.dtpckrCDSExchangeEndTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrCDSExchangeEndTime.Location = New System.Drawing.Point(265, 49)
        Me.dtpckrCDSExchangeEndTime.Name = "dtpckrCDSExchangeEndTime"
        Me.dtpckrCDSExchangeEndTime.ShowUpDown = True
        Me.dtpckrCDSExchangeEndTime.Size = New System.Drawing.Size(134, 22)
        Me.dtpckrCDSExchangeEndTime.TabIndex = 9
        Me.dtpckrCDSExchangeEndTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'lblCDSExchangeEndTime
        '
        Me.lblCDSExchangeEndTime.AutoSize = True
        Me.lblCDSExchangeEndTime.Location = New System.Drawing.Point(3, 51)
        Me.lblCDSExchangeEndTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblCDSExchangeEndTime.Name = "lblCDSExchangeEndTime"
        Me.lblCDSExchangeEndTime.Size = New System.Drawing.Size(134, 17)
        Me.lblCDSExchangeEndTime.TabIndex = 32
        Me.lblCDSExchangeEndTime.Text = "Exchange End Time"
        '
        'dtpckrCDSExchangeStartTime
        '
        Me.dtpckrCDSExchangeStartTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrCDSExchangeStartTime.Location = New System.Drawing.Point(265, 20)
        Me.dtpckrCDSExchangeStartTime.Name = "dtpckrCDSExchangeStartTime"
        Me.dtpckrCDSExchangeStartTime.ShowUpDown = True
        Me.dtpckrCDSExchangeStartTime.Size = New System.Drawing.Size(134, 22)
        Me.dtpckrCDSExchangeStartTime.TabIndex = 8
        Me.dtpckrCDSExchangeStartTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'lblCDSExchangeStartTime
        '
        Me.lblCDSExchangeStartTime.AutoSize = True
        Me.lblCDSExchangeStartTime.Location = New System.Drawing.Point(3, 22)
        Me.lblCDSExchangeStartTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblCDSExchangeStartTime.Name = "lblCDSExchangeStartTime"
        Me.lblCDSExchangeStartTime.Size = New System.Drawing.Size(139, 17)
        Me.lblCDSExchangeStartTime.TabIndex = 30
        Me.lblCDSExchangeStartTime.Text = "Exchange Start Time"
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.dtpckrMCXExchangeEndTime)
        Me.GroupBox2.Controls.Add(Me.lblMCXExchangeEndTime)
        Me.GroupBox2.Controls.Add(Me.dtpckrMCXExchangeStartTime)
        Me.GroupBox2.Controls.Add(Me.lblMCXExchangeStartTime)
        Me.GroupBox2.Location = New System.Drawing.Point(9, 94)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(404, 85)
        Me.GroupBox2.TabIndex = 30
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "MCX"
        '
        'dtpckrMCXExchangeEndTime
        '
        Me.dtpckrMCXExchangeEndTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrMCXExchangeEndTime.Location = New System.Drawing.Point(265, 49)
        Me.dtpckrMCXExchangeEndTime.Name = "dtpckrMCXExchangeEndTime"
        Me.dtpckrMCXExchangeEndTime.ShowUpDown = True
        Me.dtpckrMCXExchangeEndTime.Size = New System.Drawing.Size(134, 22)
        Me.dtpckrMCXExchangeEndTime.TabIndex = 7
        Me.dtpckrMCXExchangeEndTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'lblMCXExchangeEndTime
        '
        Me.lblMCXExchangeEndTime.AutoSize = True
        Me.lblMCXExchangeEndTime.Location = New System.Drawing.Point(3, 51)
        Me.lblMCXExchangeEndTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMCXExchangeEndTime.Name = "lblMCXExchangeEndTime"
        Me.lblMCXExchangeEndTime.Size = New System.Drawing.Size(134, 17)
        Me.lblMCXExchangeEndTime.TabIndex = 32
        Me.lblMCXExchangeEndTime.Text = "Exchange End Time"
        '
        'dtpckrMCXExchangeStartTime
        '
        Me.dtpckrMCXExchangeStartTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrMCXExchangeStartTime.Location = New System.Drawing.Point(265, 20)
        Me.dtpckrMCXExchangeStartTime.Name = "dtpckrMCXExchangeStartTime"
        Me.dtpckrMCXExchangeStartTime.ShowUpDown = True
        Me.dtpckrMCXExchangeStartTime.Size = New System.Drawing.Size(134, 22)
        Me.dtpckrMCXExchangeStartTime.TabIndex = 6
        Me.dtpckrMCXExchangeStartTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'lblMCXExchangeStartTime
        '
        Me.lblMCXExchangeStartTime.AutoSize = True
        Me.lblMCXExchangeStartTime.Location = New System.Drawing.Point(3, 22)
        Me.lblMCXExchangeStartTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblMCXExchangeStartTime.Name = "lblMCXExchangeStartTime"
        Me.lblMCXExchangeStartTime.Size = New System.Drawing.Size(139, 17)
        Me.lblMCXExchangeStartTime.TabIndex = 30
        Me.lblMCXExchangeStartTime.Text = "Exchange Start Time"
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.dtpckrNSEExchangeEndTime)
        Me.GroupBox1.Controls.Add(Me.lblNSEExcahngeEndTime)
        Me.GroupBox1.Controls.Add(Me.dtpckrNSEExchangeStartTime)
        Me.GroupBox1.Controls.Add(Me.lblNSEExchangeStartTime)
        Me.GroupBox1.Location = New System.Drawing.Point(9, 3)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(404, 85)
        Me.GroupBox1.TabIndex = 29
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "NSE"
        '
        'dtpckrNSEExchangeEndTime
        '
        Me.dtpckrNSEExchangeEndTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrNSEExchangeEndTime.Location = New System.Drawing.Point(265, 49)
        Me.dtpckrNSEExchangeEndTime.Name = "dtpckrNSEExchangeEndTime"
        Me.dtpckrNSEExchangeEndTime.ShowUpDown = True
        Me.dtpckrNSEExchangeEndTime.Size = New System.Drawing.Size(134, 22)
        Me.dtpckrNSEExchangeEndTime.TabIndex = 5
        Me.dtpckrNSEExchangeEndTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'lblNSEExcahngeEndTime
        '
        Me.lblNSEExcahngeEndTime.AutoSize = True
        Me.lblNSEExcahngeEndTime.Location = New System.Drawing.Point(3, 51)
        Me.lblNSEExcahngeEndTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblNSEExcahngeEndTime.Name = "lblNSEExcahngeEndTime"
        Me.lblNSEExcahngeEndTime.Size = New System.Drawing.Size(134, 17)
        Me.lblNSEExcahngeEndTime.TabIndex = 32
        Me.lblNSEExcahngeEndTime.Text = "Exchange End Time"
        '
        'dtpckrNSEExchangeStartTime
        '
        Me.dtpckrNSEExchangeStartTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrNSEExchangeStartTime.Location = New System.Drawing.Point(265, 20)
        Me.dtpckrNSEExchangeStartTime.Name = "dtpckrNSEExchangeStartTime"
        Me.dtpckrNSEExchangeStartTime.ShowUpDown = True
        Me.dtpckrNSEExchangeStartTime.Size = New System.Drawing.Size(134, 22)
        Me.dtpckrNSEExchangeStartTime.TabIndex = 4
        Me.dtpckrNSEExchangeStartTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'lblNSEExchangeStartTime
        '
        Me.lblNSEExchangeStartTime.AutoSize = True
        Me.lblNSEExchangeStartTime.Location = New System.Drawing.Point(3, 22)
        Me.lblNSEExchangeStartTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblNSEExchangeStartTime.Name = "lblNSEExchangeStartTime"
        Me.lblNSEExchangeStartTime.Size = New System.Drawing.Size(139, 17)
        Me.lblNSEExchangeStartTime.TabIndex = 30
        Me.lblNSEExchangeStartTime.Text = "Exchange Start Time"
        '
        'ImageList1
        '
        Me.ImageList1.ImageStream = CType(resources.GetObject("ImageList1.ImageStream"), System.Windows.Forms.ImageListStreamer)
        Me.ImageList1.TransparentColor = System.Drawing.Color.Transparent
        Me.ImageList1.Images.SetKeyName(0, "save-icon-36533.png")
        '
        'btnSaveDelaySettings
        '
        Me.btnSaveDelaySettings.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnSaveDelaySettings.ImageKey = "save-icon-36533.png"
        Me.btnSaveDelaySettings.ImageList = Me.ImageList1
        Me.btnSaveDelaySettings.Location = New System.Drawing.Point(424, 25)
        Me.btnSaveDelaySettings.Margin = New System.Windows.Forms.Padding(4)
        Me.btnSaveDelaySettings.Name = "btnSaveDelaySettings"
        Me.btnSaveDelaySettings.Size = New System.Drawing.Size(112, 58)
        Me.btnSaveDelaySettings.TabIndex = 0
        Me.btnSaveDelaySettings.Text = "&Save"
        Me.btnSaveDelaySettings.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnSaveDelaySettings.UseVisualStyleBackColor = True
        '
        'tabRemarks
        '
        Me.tabRemarks.Controls.Add(Me.txtRemarks)
        Me.tabRemarks.Controls.Add(Me.lblRemarks)
        Me.tabRemarks.Location = New System.Drawing.Point(4, 25)
        Me.tabRemarks.Name = "tabRemarks"
        Me.tabRemarks.Size = New System.Drawing.Size(416, 282)
        Me.tabRemarks.TabIndex = 2
        Me.tabRemarks.Text = "Remarks"
        Me.tabRemarks.UseVisualStyleBackColor = True
        '
        'txtRemarks
        '
        Me.txtRemarks.Location = New System.Drawing.Point(117, 20)
        Me.txtRemarks.Margin = New System.Windows.Forms.Padding(4)
        Me.txtRemarks.Name = "txtRemarks"
        Me.txtRemarks.Size = New System.Drawing.Size(288, 22)
        Me.txtRemarks.TabIndex = 11
        '
        'lblRemarks
        '
        Me.lblRemarks.AutoSize = True
        Me.lblRemarks.Location = New System.Drawing.Point(9, 21)
        Me.lblRemarks.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblRemarks.Name = "lblRemarks"
        Me.lblRemarks.Size = New System.Drawing.Size(100, 17)
        Me.lblRemarks.TabIndex = 12
        Me.lblRemarks.Text = "Form Remarks"
        '
        'frmAdvancedOptions
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(541, 311)
        Me.Controls.Add(Me.btnSaveDelaySettings)
        Me.Controls.Add(Me.tabMain)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "frmAdvancedOptions"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Advanced Options"
        Me.tabMain.ResumeLayout(False)
        Me.tabDelaySettings.ResumeLayout(False)
        Me.tabDelaySettings.PerformLayout()
        Me.tabExchangeDetailsSettings.ResumeLayout(False)
        Me.GroupBox3.ResumeLayout(False)
        Me.GroupBox3.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.tabRemarks.ResumeLayout(False)
        Me.tabRemarks.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents tabMain As TabControl
    Friend WithEvents tabDelaySettings As TabPage
    Friend WithEvents ImageList1 As ImageList
    Friend WithEvents txtGetInformationDelay As TextBox
    Friend WithEvents lblGetInformationDelay As Label
    Friend WithEvents txtBackToBackOrderCoolOffDelay As TextBox
    Friend WithEvents lblBackToBackOrderCoolOffDelay As Label
    Friend WithEvents dtpckrForceRestartTime As DateTimePicker
    Friend WithEvents lblForceRestartTime As Label
    Friend WithEvents tabExchangeDetailsSettings As TabPage
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents dtpckrNSEExchangeStartTime As DateTimePicker
    Friend WithEvents lblNSEExchangeStartTime As Label
    Friend WithEvents dtpckrNSEExchangeEndTime As DateTimePicker
    Friend WithEvents lblNSEExcahngeEndTime As Label
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents dtpckrMCXExchangeEndTime As DateTimePicker
    Friend WithEvents lblMCXExchangeEndTime As Label
    Friend WithEvents dtpckrMCXExchangeStartTime As DateTimePicker
    Friend WithEvents lblMCXExchangeStartTime As Label
    Friend WithEvents GroupBox3 As GroupBox
    Friend WithEvents dtpckrCDSExchangeEndTime As DateTimePicker
    Friend WithEvents lblCDSExchangeEndTime As Label
    Friend WithEvents dtpckrCDSExchangeStartTime As DateTimePicker
    Friend WithEvents lblCDSExchangeStartTime As Label
    Friend WithEvents btnSaveDelaySettings As Button
    Friend WithEvents tabRemarks As TabPage
    Friend WithEvents txtRemarks As TextBox
    Friend WithEvents lblRemarks As Label
End Class
