<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class frmMain
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
        Me.btnStart = New System.Windows.Forms.Button()
        Me.tmrTickerStatus = New System.Windows.Forms.Timer(Me.components)
        Me.lblTickerStatus = New System.Windows.Forms.Label()
        Me.blbTickerStatus = New Bulb.LedBulb()
        Me.dgMainDashboard = New System.Windows.Forms.DataGridView()
        Me.sfdgvMainDashboard = New Syncfusion.WinForms.DataGrid.SfDataGrid()
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.pnlVerticalDivider = New System.Windows.Forms.TableLayoutPanel()
        Me.pnlLeftPartHorizontalDivider = New System.Windows.Forms.TableLayoutPanel()
        Me.lstLog = New System.Windows.Forms.ListBox()
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.CredentialsToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.UserDetailsToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.FlowLayoutPanel1 = New System.Windows.Forms.FlowLayoutPanel()
        CType(Me.dgMainDashboard, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.sfdgvMainDashboard, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel1.SuspendLayout()
        Me.pnlVerticalDivider.SuspendLayout()
        Me.pnlLeftPartHorizontalDivider.SuspendLayout()
        Me.MenuStrip1.SuspendLayout()
        Me.FlowLayoutPanel1.SuspendLayout()
        Me.SuspendLayout()
        '
        'btnStart
        '
        Me.btnStart.Location = New System.Drawing.Point(3, 32)
        Me.btnStart.Name = "btnStart"
        Me.btnStart.Size = New System.Drawing.Size(75, 23)
        Me.btnStart.TabIndex = 1
        Me.btnStart.Text = "Start"
        Me.btnStart.UseVisualStyleBackColor = True
        '
        'tmrTickerStatus
        '
        Me.tmrTickerStatus.Enabled = True
        '
        'lblTickerStatus
        '
        Me.lblTickerStatus.AutoSize = True
        Me.lblTickerStatus.Location = New System.Drawing.Point(3, 0)
        Me.lblTickerStatus.Name = "lblTickerStatus"
        Me.lblTickerStatus.Size = New System.Drawing.Size(70, 13)
        Me.lblTickerStatus.TabIndex = 2
        Me.lblTickerStatus.Text = "Ticker Status"
        '
        'blbTickerStatus
        '
        Me.blbTickerStatus.Color = System.Drawing.Color.Pink
        Me.blbTickerStatus.Location = New System.Drawing.Point(79, 3)
        Me.blbTickerStatus.Name = "blbTickerStatus"
        Me.blbTickerStatus.On = True
        Me.blbTickerStatus.Size = New System.Drawing.Size(75, 23)
        Me.blbTickerStatus.TabIndex = 3
        Me.blbTickerStatus.Text = "LedBulb1"
        '
        'dgMainDashboard
        '
        Me.dgMainDashboard.AllowUserToAddRows = False
        Me.dgMainDashboard.AllowUserToDeleteRows = False
        Me.dgMainDashboard.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgMainDashboard.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically
        Me.dgMainDashboard.Location = New System.Drawing.Point(912, 467)
        Me.dgMainDashboard.MultiSelect = False
        Me.dgMainDashboard.Name = "dgMainDashboard"
        Me.dgMainDashboard.ReadOnly = True
        Me.dgMainDashboard.RowHeadersVisible = False
        Me.dgMainDashboard.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.dgMainDashboard.Size = New System.Drawing.Size(1011, 251)
        Me.dgMainDashboard.TabIndex = 4
        Me.dgMainDashboard.Visible = False
        '
        'sfdgvMainDashboard
        '
        Me.sfdgvMainDashboard.AccessibleName = "Table"
        Me.sfdgvMainDashboard.AllowDraggingColumns = True
        Me.sfdgvMainDashboard.AllowEditing = False
        Me.sfdgvMainDashboard.AllowFiltering = True
        Me.sfdgvMainDashboard.AllowResizingColumns = True
        Me.sfdgvMainDashboard.AutoSizeColumnsMode = Syncfusion.WinForms.DataGrid.Enums.AutoSizeColumnsMode.AllCells
        Me.sfdgvMainDashboard.Dock = System.Windows.Forms.DockStyle.Fill
        Me.sfdgvMainDashboard.Location = New System.Drawing.Point(3, 54)
        Me.sfdgvMainDashboard.Name = "sfdgvMainDashboard"
        Me.sfdgvMainDashboard.PasteOption = Syncfusion.WinForms.DataGrid.Enums.PasteOptions.None
        Me.sfdgvMainDashboard.Size = New System.Drawing.Size(941, 522)
        Me.sfdgvMainDashboard.TabIndex = 5
        Me.sfdgvMainDashboard.Text = "SfDataGrid1"
        '
        'Panel1
        '
        Me.Panel1.Controls.Add(Me.pnlVerticalDivider)
        Me.Panel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Panel1.Location = New System.Drawing.Point(0, 0)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(1362, 741)
        Me.Panel1.TabIndex = 6
        '
        'pnlVerticalDivider
        '
        Me.pnlVerticalDivider.ColumnCount = 2
        Me.pnlVerticalDivider.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70.0!))
        Me.pnlVerticalDivider.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30.0!))
        Me.pnlVerticalDivider.Controls.Add(Me.pnlLeftPartHorizontalDivider, 0, 0)
        Me.pnlVerticalDivider.Controls.Add(Me.FlowLayoutPanel1, 1, 0)
        Me.pnlVerticalDivider.Dock = System.Windows.Forms.DockStyle.Fill
        Me.pnlVerticalDivider.Location = New System.Drawing.Point(0, 0)
        Me.pnlVerticalDivider.Name = "pnlVerticalDivider"
        Me.pnlVerticalDivider.RowCount = 1
        Me.pnlVerticalDivider.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.pnlVerticalDivider.Size = New System.Drawing.Size(1362, 741)
        Me.pnlVerticalDivider.TabIndex = 0
        '
        'pnlLeftPartHorizontalDivider
        '
        Me.pnlLeftPartHorizontalDivider.ColumnCount = 1
        Me.pnlLeftPartHorizontalDivider.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.pnlLeftPartHorizontalDivider.Controls.Add(Me.lstLog, 0, 2)
        Me.pnlLeftPartHorizontalDivider.Controls.Add(Me.sfdgvMainDashboard, 0, 1)
        Me.pnlLeftPartHorizontalDivider.Controls.Add(Me.MenuStrip1, 0, 0)
        Me.pnlLeftPartHorizontalDivider.Dock = System.Windows.Forms.DockStyle.Fill
        Me.pnlLeftPartHorizontalDivider.Location = New System.Drawing.Point(3, 3)
        Me.pnlLeftPartHorizontalDivider.Name = "pnlLeftPartHorizontalDivider"
        Me.pnlLeftPartHorizontalDivider.RowCount = 3
        Me.pnlLeftPartHorizontalDivider.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.938776!))
        Me.pnlLeftPartHorizontalDivider.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 71.83673!))
        Me.pnlLeftPartHorizontalDivider.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 21.05263!))
        Me.pnlLeftPartHorizontalDivider.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.pnlLeftPartHorizontalDivider.Size = New System.Drawing.Size(947, 735)
        Me.pnlLeftPartHorizontalDivider.TabIndex = 0
        '
        'lstLog
        '
        Me.lstLog.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lstLog.FormattingEnabled = True
        Me.lstLog.Location = New System.Drawing.Point(3, 582)
        Me.lstLog.Name = "lstLog"
        Me.lstLog.Size = New System.Drawing.Size(941, 150)
        Me.lstLog.TabIndex = 8
        '
        'MenuStrip1
        '
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.CredentialsToolStripMenuItem})
        Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.Size = New System.Drawing.Size(947, 24)
        Me.MenuStrip1.TabIndex = 9
        Me.MenuStrip1.Text = "MenuStrip1"
        '
        'CredentialsToolStripMenuItem
        '
        Me.CredentialsToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.UserDetailsToolStripMenuItem})
        Me.CredentialsToolStripMenuItem.Name = "CredentialsToolStripMenuItem"
        Me.CredentialsToolStripMenuItem.Size = New System.Drawing.Size(78, 20)
        Me.CredentialsToolStripMenuItem.Text = "Credentials"
        '
        'UserDetailsToolStripMenuItem
        '
        Me.UserDetailsToolStripMenuItem.Name = "UserDetailsToolStripMenuItem"
        Me.UserDetailsToolStripMenuItem.Size = New System.Drawing.Size(135, 22)
        Me.UserDetailsToolStripMenuItem.Text = "User Details"
        '
        'FlowLayoutPanel1
        '
        Me.FlowLayoutPanel1.Controls.Add(Me.lblTickerStatus)
        Me.FlowLayoutPanel1.Controls.Add(Me.blbTickerStatus)
        Me.FlowLayoutPanel1.Controls.Add(Me.btnStart)
        Me.FlowLayoutPanel1.Location = New System.Drawing.Point(956, 3)
        Me.FlowLayoutPanel1.Name = "FlowLayoutPanel1"
        Me.FlowLayoutPanel1.Size = New System.Drawing.Size(200, 100)
        Me.FlowLayoutPanel1.TabIndex = 1
        '
        'frmMain
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1362, 741)
        Me.Controls.Add(Me.dgMainDashboard)
        Me.Controls.Add(Me.Panel1)
        Me.MainMenuStrip = Me.MenuStrip1
        Me.Name = "frmMain"
        Me.Text = "Form1"
        CType(Me.dgMainDashboard, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.sfdgvMainDashboard, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel1.ResumeLayout(False)
        Me.pnlVerticalDivider.ResumeLayout(False)
        Me.pnlLeftPartHorizontalDivider.ResumeLayout(False)
        Me.pnlLeftPartHorizontalDivider.PerformLayout()
        Me.MenuStrip1.ResumeLayout(False)
        Me.MenuStrip1.PerformLayout()
        Me.FlowLayoutPanel1.ResumeLayout(False)
        Me.FlowLayoutPanel1.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents btnStart As Button
    Friend WithEvents lblTickerStatus As Label
    Friend WithEvents blbTickerStatus As Bulb.LedBulb
    Friend WithEvents dgMainDashboard As DataGridView

    Public Sub New()
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("NTU1MDhAMzEzNjJlMzQyZTMwTlptNGNQRDRvUUM0U2FFUU10NjdpYmQ1WGIvbUEvZDVHUEdOdGduL2hSTT0=")
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

    End Sub

    Friend WithEvents sfdgvMainDashboard As Syncfusion.WinForms.DataGrid.SfDataGrid
    Friend WithEvents pnlVerticalDivider As TableLayoutPanel
    Friend WithEvents pnlLeftPartHorizontalDivider As TableLayoutPanel
    Friend WithEvents lstLog As ListBox
    Friend WithEvents FlowLayoutPanel1 As FlowLayoutPanel
    Private WithEvents Panel1 As Panel
    Friend WithEvents MenuStrip1 As MenuStrip
    Friend WithEvents CredentialsToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents UserDetailsToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents tmrTickerStatus As Timer
End Class
