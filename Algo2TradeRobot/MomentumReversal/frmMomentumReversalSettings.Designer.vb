﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
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
        Me.txtBackToBackTradeTimeGap = New System.Windows.Forms.TextBox()
        Me.lblBackToBackTradeTimeGap = New System.Windows.Forms.Label()
        Me.dtpckrIdleTimeEnd = New System.Windows.Forms.DateTimePicker()
        Me.dtpckrIdleTimeStart = New System.Windows.Forms.DateTimePicker()
        Me.lblIdleTimeEnd = New System.Windows.Forms.Label()
        Me.lblIdleTimeStart = New System.Windows.Forms.Label()
        Me.txtTradeOpenTime = New System.Windows.Forms.TextBox()
        Me.lblTradeOpenTime = New System.Windows.Forms.Label()
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
        Me.lblRSIPeriod = New System.Windows.Forms.Label()
        Me.txtRSIPeriod = New System.Windows.Forms.TextBox()
        Me.lblRSILevel = New System.Windows.Forms.Label()
        Me.txtRSILevel = New System.Windows.Forms.TextBox()
        Me.grpRSI = New System.Windows.Forms.GroupBox()
        Me.GroupBox1.SuspendLayout()
        Me.grpRSI.SuspendLayout()
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
        Me.GroupBox1.Controls.Add(Me.txtBackToBackTradeTimeGap)
        Me.GroupBox1.Controls.Add(Me.lblBackToBackTradeTimeGap)
        Me.GroupBox1.Controls.Add(Me.dtpckrIdleTimeEnd)
        Me.GroupBox1.Controls.Add(Me.dtpckrIdleTimeStart)
        Me.GroupBox1.Controls.Add(Me.lblIdleTimeEnd)
        Me.GroupBox1.Controls.Add(Me.lblIdleTimeStart)
        Me.GroupBox1.Controls.Add(Me.txtTradeOpenTime)
        Me.GroupBox1.Controls.Add(Me.lblTradeOpenTime)
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
        Me.GroupBox1.Size = New System.Drawing.Size(451, 328)
        Me.GroupBox1.TabIndex = 22
        Me.GroupBox1.TabStop = False
        '
        'txtBackToBackTradeTimeGap
        '
        Me.txtBackToBackTradeTimeGap.Location = New System.Drawing.Point(189, 85)
        Me.txtBackToBackTradeTimeGap.Margin = New System.Windows.Forms.Padding(4)
        Me.txtBackToBackTradeTimeGap.Name = "txtBackToBackTradeTimeGap"
        Me.txtBackToBackTradeTimeGap.Size = New System.Drawing.Size(241, 22)
        Me.txtBackToBackTradeTimeGap.TabIndex = 3
        Me.txtBackToBackTradeTimeGap.Tag = "Back To Back Trade Time Gap"
        '
        'lblBackToBackTradeTimeGap
        '
        Me.lblBackToBackTradeTimeGap.AutoSize = True
        Me.lblBackToBackTradeTimeGap.Location = New System.Drawing.Point(8, 90)
        Me.lblBackToBackTradeTimeGap.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblBackToBackTradeTimeGap.Name = "lblBackToBackTradeTimeGap"
        Me.lblBackToBackTradeTimeGap.Size = New System.Drawing.Size(174, 17)
        Me.lblBackToBackTradeTimeGap.TabIndex = 39
        Me.lblBackToBackTradeTimeGap.Tag = ""
        Me.lblBackToBackTradeTimeGap.Text = "B2B Trade Time Gap(sec)"
        '
        'dtpckrIdleTimeEnd
        '
        Me.dtpckrIdleTimeEnd.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrIdleTimeEnd.Location = New System.Drawing.Point(189, 264)
        Me.dtpckrIdleTimeEnd.Name = "dtpckrIdleTimeEnd"
        Me.dtpckrIdleTimeEnd.ShowUpDown = True
        Me.dtpckrIdleTimeEnd.Size = New System.Drawing.Size(242, 22)
        Me.dtpckrIdleTimeEnd.TabIndex = 8
        Me.dtpckrIdleTimeEnd.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'dtpckrIdleTimeStart
        '
        Me.dtpckrIdleTimeStart.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrIdleTimeStart.Location = New System.Drawing.Point(189, 227)
        Me.dtpckrIdleTimeStart.Name = "dtpckrIdleTimeStart"
        Me.dtpckrIdleTimeStart.ShowUpDown = True
        Me.dtpckrIdleTimeStart.Size = New System.Drawing.Size(241, 22)
        Me.dtpckrIdleTimeStart.TabIndex = 7
        Me.dtpckrIdleTimeStart.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'lblIdleTimeEnd
        '
        Me.lblIdleTimeEnd.AutoSize = True
        Me.lblIdleTimeEnd.Location = New System.Drawing.Point(9, 265)
        Me.lblIdleTimeEnd.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblIdleTimeEnd.Name = "lblIdleTimeEnd"
        Me.lblIdleTimeEnd.Size = New System.Drawing.Size(94, 17)
        Me.lblIdleTimeEnd.TabIndex = 37
        Me.lblIdleTimeEnd.Text = "Idle Time End"
        '
        'lblIdleTimeStart
        '
        Me.lblIdleTimeStart.AutoSize = True
        Me.lblIdleTimeStart.Location = New System.Drawing.Point(9, 229)
        Me.lblIdleTimeStart.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblIdleTimeStart.Name = "lblIdleTimeStart"
        Me.lblIdleTimeStart.Size = New System.Drawing.Size(99, 17)
        Me.lblIdleTimeStart.TabIndex = 36
        Me.lblIdleTimeStart.Text = "Idle Time Start"
        '
        'txtTradeOpenTime
        '
        Me.txtTradeOpenTime.Location = New System.Drawing.Point(189, 50)
        Me.txtTradeOpenTime.Margin = New System.Windows.Forms.Padding(4)
        Me.txtTradeOpenTime.Name = "txtTradeOpenTime"
        Me.txtTradeOpenTime.Size = New System.Drawing.Size(241, 22)
        Me.txtTradeOpenTime.TabIndex = 2
        Me.txtTradeOpenTime.Tag = "Trade Open Time"
        '
        'lblTradeOpenTime
        '
        Me.lblTradeOpenTime.AutoSize = True
        Me.lblTradeOpenTime.Location = New System.Drawing.Point(8, 55)
        Me.lblTradeOpenTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTradeOpenTime.Name = "lblTradeOpenTime"
        Me.lblTradeOpenTime.Size = New System.Drawing.Size(152, 17)
        Me.lblTradeOpenTime.TabIndex = 33
        Me.lblTradeOpenTime.Tag = ""
        Me.lblTradeOpenTime.Text = "Trade Open Time(min)"
        '
        'dtpckrEODExitTime
        '
        Me.dtpckrEODExitTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrEODExitTime.Location = New System.Drawing.Point(189, 189)
        Me.dtpckrEODExitTime.Name = "dtpckrEODExitTime"
        Me.dtpckrEODExitTime.ShowUpDown = True
        Me.dtpckrEODExitTime.Size = New System.Drawing.Size(241, 22)
        Me.dtpckrEODExitTime.TabIndex = 6
        Me.dtpckrEODExitTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'dtpckrLastTradeEntryTime
        '
        Me.dtpckrLastTradeEntryTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrLastTradeEntryTime.Location = New System.Drawing.Point(189, 154)
        Me.dtpckrLastTradeEntryTime.Name = "dtpckrLastTradeEntryTime"
        Me.dtpckrLastTradeEntryTime.ShowUpDown = True
        Me.dtpckrLastTradeEntryTime.Size = New System.Drawing.Size(242, 22)
        Me.dtpckrLastTradeEntryTime.TabIndex = 5
        Me.dtpckrLastTradeEntryTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'dtpckrTradeStartTime
        '
        Me.dtpckrTradeStartTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpckrTradeStartTime.Location = New System.Drawing.Point(189, 119)
        Me.dtpckrTradeStartTime.Name = "dtpckrTradeStartTime"
        Me.dtpckrTradeStartTime.ShowUpDown = True
        Me.dtpckrTradeStartTime.Size = New System.Drawing.Size(241, 22)
        Me.dtpckrTradeStartTime.TabIndex = 4
        Me.dtpckrTradeStartTime.Value = New Date(2019, 3, 12, 0, 0, 0, 0)
        '
        'lblEODExitTime
        '
        Me.lblEODExitTime.AutoSize = True
        Me.lblEODExitTime.Location = New System.Drawing.Point(9, 190)
        Me.lblEODExitTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblEODExitTime.Name = "lblEODExitTime"
        Me.lblEODExitTime.Size = New System.Drawing.Size(99, 17)
        Me.lblEODExitTime.TabIndex = 23
        Me.lblEODExitTime.Text = "EOD Exit Time"
        '
        'lblLastTradeEntryTime
        '
        Me.lblLastTradeEntryTime.AutoSize = True
        Me.lblLastTradeEntryTime.Location = New System.Drawing.Point(9, 155)
        Me.lblLastTradeEntryTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblLastTradeEntryTime.Name = "lblLastTradeEntryTime"
        Me.lblLastTradeEntryTime.Size = New System.Drawing.Size(149, 17)
        Me.lblLastTradeEntryTime.TabIndex = 21
        Me.lblLastTradeEntryTime.Text = "Last Trade Entry Time"
        '
        'lblTradeStartTime
        '
        Me.lblTradeStartTime.AutoSize = True
        Me.lblTradeStartTime.Location = New System.Drawing.Point(9, 121)
        Me.lblTradeStartTime.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTradeStartTime.Name = "lblTradeStartTime"
        Me.lblTradeStartTime.Size = New System.Drawing.Size(115, 17)
        Me.lblTradeStartTime.TabIndex = 19
        Me.lblTradeStartTime.Text = "Trade Start Time"
        '
        'btnBrowse
        '
        Me.btnBrowse.Font = New System.Drawing.Font("Microsoft Sans Serif", 7.8!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnBrowse.Location = New System.Drawing.Point(404, 298)
        Me.btnBrowse.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.btnBrowse.Name = "btnBrowse"
        Me.btnBrowse.Size = New System.Drawing.Size(40, 23)
        Me.btnBrowse.TabIndex = 9
        Me.btnBrowse.Text = "..."
        Me.btnBrowse.UseVisualStyleBackColor = True
        '
        'txtInstrumentDetalis
        '
        Me.txtInstrumentDetalis.Location = New System.Drawing.Point(189, 299)
        Me.txtInstrumentDetalis.Margin = New System.Windows.Forms.Padding(4)
        Me.txtInstrumentDetalis.Name = "txtInstrumentDetalis"
        Me.txtInstrumentDetalis.ReadOnly = True
        Me.txtInstrumentDetalis.Size = New System.Drawing.Size(208, 22)
        Me.txtInstrumentDetalis.TabIndex = 15
        '
        'lblInstrumentDetails
        '
        Me.lblInstrumentDetails.AutoSize = True
        Me.lblInstrumentDetails.Location = New System.Drawing.Point(8, 302)
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
        'lblRSIPeriod
        '
        Me.lblRSIPeriod.AutoSize = True
        Me.lblRSIPeriod.Location = New System.Drawing.Point(9, 26)
        Me.lblRSIPeriod.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblRSIPeriod.Name = "lblRSIPeriod"
        Me.lblRSIPeriod.Size = New System.Drawing.Size(75, 17)
        Me.lblRSIPeriod.TabIndex = 35
        Me.lblRSIPeriod.Text = "RSI Period"
        '
        'txtRSIPeriod
        '
        Me.txtRSIPeriod.Location = New System.Drawing.Point(189, 21)
        Me.txtRSIPeriod.Margin = New System.Windows.Forms.Padding(4)
        Me.txtRSIPeriod.Name = "txtRSIPeriod"
        Me.txtRSIPeriod.Size = New System.Drawing.Size(241, 22)
        Me.txtRSIPeriod.TabIndex = 10
        Me.txtRSIPeriod.Tag = "RSI Period"
        '
        'lblRSILevel
        '
        Me.lblRSILevel.AutoSize = True
        Me.lblRSILevel.Location = New System.Drawing.Point(9, 58)
        Me.lblRSILevel.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblRSILevel.Name = "lblRSILevel"
        Me.lblRSILevel.Size = New System.Drawing.Size(68, 17)
        Me.lblRSILevel.TabIndex = 37
        Me.lblRSILevel.Text = "RSI Level"
        '
        'txtRSILevel
        '
        Me.txtRSILevel.Location = New System.Drawing.Point(189, 53)
        Me.txtRSILevel.Margin = New System.Windows.Forms.Padding(4)
        Me.txtRSILevel.Name = "txtRSILevel"
        Me.txtRSILevel.Size = New System.Drawing.Size(241, 22)
        Me.txtRSILevel.TabIndex = 11
        Me.txtRSILevel.Tag = "RSI Level"
        '
        'grpRSI
        '
        Me.grpRSI.Controls.Add(Me.txtRSILevel)
        Me.grpRSI.Controls.Add(Me.lblRSILevel)
        Me.grpRSI.Controls.Add(Me.txtRSIPeriod)
        Me.grpRSI.Controls.Add(Me.lblRSIPeriod)
        Me.grpRSI.Location = New System.Drawing.Point(2, 332)
        Me.grpRSI.Name = "grpRSI"
        Me.grpRSI.Size = New System.Drawing.Size(451, 87)
        Me.grpRSI.TabIndex = 23
        Me.grpRSI.TabStop = False
        Me.grpRSI.Text = "RSI Settings"
        '
        'frmMomentumReversalSettings
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(576, 422)
        Me.Controls.Add(Me.grpRSI)
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
        Me.grpRSI.ResumeLayout(False)
        Me.grpRSI.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents btnSaveMomentumReversalSettings As Button
    Friend WithEvents ImageList1 As ImageList
    Friend WithEvents opnFileSettings As OpenFileDialog
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents dtpckrIdleTimeEnd As DateTimePicker
    Friend WithEvents dtpckrIdleTimeStart As DateTimePicker
    Friend WithEvents lblIdleTimeEnd As Label
    Friend WithEvents lblIdleTimeStart As Label
    Friend WithEvents txtTradeOpenTime As TextBox
    Friend WithEvents lblTradeOpenTime As Label
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
    Friend WithEvents txtBackToBackTradeTimeGap As TextBox
    Friend WithEvents lblBackToBackTradeTimeGap As Label
    Friend WithEvents lblRSIPeriod As Label
    Friend WithEvents txtRSIPeriod As TextBox
    Friend WithEvents lblRSILevel As Label
    Friend WithEvents txtRSILevel As TextBox
    Friend WithEvents grpRSI As GroupBox
End Class
