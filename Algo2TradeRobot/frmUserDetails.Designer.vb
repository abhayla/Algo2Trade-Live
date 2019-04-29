<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmZerodhaUserDetails
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmZerodhaUserDetails))
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.txtZerodhaPin = New System.Windows.Forms.TextBox()
        Me.lblZerodhaPin = New System.Windows.Forms.Label()
        Me.txtZerodhaAPISecret = New System.Windows.Forms.TextBox()
        Me.txtZerodhaAPIKey = New System.Windows.Forms.TextBox()
        Me.txtZerodhaPassword = New System.Windows.Forms.TextBox()
        Me.txtZerodhaUserId = New System.Windows.Forms.TextBox()
        Me.lblZerodhaAPISecret = New System.Windows.Forms.Label()
        Me.lblZerodhaAPIKey = New System.Windows.Forms.Label()
        Me.lblZerodhaPassword = New System.Windows.Forms.Label()
        Me.lblZerodhaUserId = New System.Windows.Forms.Label()
        Me.btnSaveZerodhaUserDetails = New System.Windows.Forms.Button()
        Me.ImageList1 = New System.Windows.Forms.ImageList(Me.components)
        Me.GroupBox1.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.txtZerodhaPin)
        Me.GroupBox1.Controls.Add(Me.lblZerodhaPin)
        Me.GroupBox1.Controls.Add(Me.txtZerodhaAPISecret)
        Me.GroupBox1.Controls.Add(Me.txtZerodhaAPIKey)
        Me.GroupBox1.Controls.Add(Me.txtZerodhaPassword)
        Me.GroupBox1.Controls.Add(Me.txtZerodhaUserId)
        Me.GroupBox1.Controls.Add(Me.lblZerodhaAPISecret)
        Me.GroupBox1.Controls.Add(Me.lblZerodhaAPIKey)
        Me.GroupBox1.Controls.Add(Me.lblZerodhaPassword)
        Me.GroupBox1.Controls.Add(Me.lblZerodhaUserId)
        Me.GroupBox1.Location = New System.Drawing.Point(9, 7)
        Me.GroupBox1.Margin = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Padding = New System.Windows.Forms.Padding(4)
        Me.GroupBox1.Size = New System.Drawing.Size(460, 206)
        Me.GroupBox1.TabIndex = 0
        Me.GroupBox1.TabStop = False
        '
        'txtZerodhaPin
        '
        Me.txtZerodhaPin.Location = New System.Drawing.Point(103, 96)
        Me.txtZerodhaPin.Margin = New System.Windows.Forms.Padding(4)
        Me.txtZerodhaPin.MaxLength = 6
        Me.txtZerodhaPin.Name = "txtZerodhaPin"
        Me.txtZerodhaPin.PasswordChar = Global.Microsoft.VisualBasic.ChrW(36)
        Me.txtZerodhaPin.Size = New System.Drawing.Size(185, 22)
        Me.txtZerodhaPin.TabIndex = 7
        '
        'lblZerodhaPin
        '
        Me.lblZerodhaPin.AutoSize = True
        Me.lblZerodhaPin.Location = New System.Drawing.Point(9, 100)
        Me.lblZerodhaPin.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblZerodhaPin.Name = "lblZerodhaPin"
        Me.lblZerodhaPin.Size = New System.Drawing.Size(57, 17)
        Me.lblZerodhaPin.TabIndex = 2
        Me.lblZerodhaPin.Text = "2FA Pin"
        '
        'txtZerodhaAPISecret
        '
        Me.txtZerodhaAPISecret.Location = New System.Drawing.Point(103, 171)
        Me.txtZerodhaAPISecret.Margin = New System.Windows.Forms.Padding(4)
        Me.txtZerodhaAPISecret.Name = "txtZerodhaAPISecret"
        Me.txtZerodhaAPISecret.Size = New System.Drawing.Size(348, 22)
        Me.txtZerodhaAPISecret.TabIndex = 9
        '
        'txtZerodhaAPIKey
        '
        Me.txtZerodhaAPIKey.Location = New System.Drawing.Point(103, 133)
        Me.txtZerodhaAPIKey.Margin = New System.Windows.Forms.Padding(4)
        Me.txtZerodhaAPIKey.Name = "txtZerodhaAPIKey"
        Me.txtZerodhaAPIKey.Size = New System.Drawing.Size(348, 22)
        Me.txtZerodhaAPIKey.TabIndex = 8
        '
        'txtZerodhaPassword
        '
        Me.txtZerodhaPassword.Location = New System.Drawing.Point(103, 60)
        Me.txtZerodhaPassword.Margin = New System.Windows.Forms.Padding(4)
        Me.txtZerodhaPassword.Name = "txtZerodhaPassword"
        Me.txtZerodhaPassword.PasswordChar = Global.Microsoft.VisualBasic.ChrW(36)
        Me.txtZerodhaPassword.Size = New System.Drawing.Size(185, 22)
        Me.txtZerodhaPassword.TabIndex = 6
        '
        'txtZerodhaUserId
        '
        Me.txtZerodhaUserId.Location = New System.Drawing.Point(103, 21)
        Me.txtZerodhaUserId.Margin = New System.Windows.Forms.Padding(4)
        Me.txtZerodhaUserId.Name = "txtZerodhaUserId"
        Me.txtZerodhaUserId.Size = New System.Drawing.Size(185, 22)
        Me.txtZerodhaUserId.TabIndex = 5
        '
        'lblZerodhaAPISecret
        '
        Me.lblZerodhaAPISecret.AutoSize = True
        Me.lblZerodhaAPISecret.Location = New System.Drawing.Point(9, 175)
        Me.lblZerodhaAPISecret.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblZerodhaAPISecret.Name = "lblZerodhaAPISecret"
        Me.lblZerodhaAPISecret.Size = New System.Drawing.Size(74, 17)
        Me.lblZerodhaAPISecret.TabIndex = 4
        Me.lblZerodhaAPISecret.Text = "API Secret"
        '
        'lblZerodhaAPIKey
        '
        Me.lblZerodhaAPIKey.AutoSize = True
        Me.lblZerodhaAPIKey.Location = New System.Drawing.Point(9, 136)
        Me.lblZerodhaAPIKey.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblZerodhaAPIKey.Name = "lblZerodhaAPIKey"
        Me.lblZerodhaAPIKey.Size = New System.Drawing.Size(57, 17)
        Me.lblZerodhaAPIKey.TabIndex = 3
        Me.lblZerodhaAPIKey.Text = "API Key"
        '
        'lblZerodhaPassword
        '
        Me.lblZerodhaPassword.AutoSize = True
        Me.lblZerodhaPassword.Location = New System.Drawing.Point(9, 64)
        Me.lblZerodhaPassword.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblZerodhaPassword.Name = "lblZerodhaPassword"
        Me.lblZerodhaPassword.Size = New System.Drawing.Size(69, 17)
        Me.lblZerodhaPassword.TabIndex = 1
        Me.lblZerodhaPassword.Text = "Password"
        '
        'lblZerodhaUserId
        '
        Me.lblZerodhaUserId.AutoSize = True
        Me.lblZerodhaUserId.Location = New System.Drawing.Point(9, 25)
        Me.lblZerodhaUserId.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblZerodhaUserId.Name = "lblZerodhaUserId"
        Me.lblZerodhaUserId.Size = New System.Drawing.Size(49, 17)
        Me.lblZerodhaUserId.TabIndex = 0
        Me.lblZerodhaUserId.Text = "UserId"
        '
        'btnSaveZerodhaUserDetails
        '
        Me.btnSaveZerodhaUserDetails.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnSaveZerodhaUserDetails.ImageKey = "save-icon-36533.png"
        Me.btnSaveZerodhaUserDetails.ImageList = Me.ImageList1
        Me.btnSaveZerodhaUserDetails.Location = New System.Drawing.Point(480, 16)
        Me.btnSaveZerodhaUserDetails.Margin = New System.Windows.Forms.Padding(4)
        Me.btnSaveZerodhaUserDetails.Name = "btnSaveZerodhaUserDetails"
        Me.btnSaveZerodhaUserDetails.Size = New System.Drawing.Size(112, 58)
        Me.btnSaveZerodhaUserDetails.TabIndex = 1
        Me.btnSaveZerodhaUserDetails.Text = "&Save"
        Me.btnSaveZerodhaUserDetails.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnSaveZerodhaUserDetails.UseVisualStyleBackColor = True
        '
        'ImageList1
        '
        Me.ImageList1.ImageStream = CType(resources.GetObject("ImageList1.ImageStream"), System.Windows.Forms.ImageListStreamer)
        Me.ImageList1.TransparentColor = System.Drawing.Color.Transparent
        Me.ImageList1.Images.SetKeyName(0, "save-icon-36533.png")
        '
        'frmZerodhaUserDetails
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(603, 226)
        Me.Controls.Add(Me.btnSaveZerodhaUserDetails)
        Me.Controls.Add(Me.GroupBox1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Margin = New System.Windows.Forms.Padding(4)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "frmZerodhaUserDetails"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Zerodha User Details"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents lblZerodhaUserId As Label
    Friend WithEvents lblZerodhaAPIKey As Label
    Friend WithEvents lblZerodhaPassword As Label
    Friend WithEvents txtZerodhaAPISecret As TextBox
    Friend WithEvents txtZerodhaAPIKey As TextBox
    Friend WithEvents txtZerodhaPassword As TextBox
    Friend WithEvents txtZerodhaUserId As TextBox
    Friend WithEvents lblZerodhaAPISecret As Label
    Friend WithEvents btnSaveZerodhaUserDetails As Button
    Friend WithEvents ImageList1 As ImageList
    Friend WithEvents txtZerodhaPin As TextBox
    Friend WithEvents lblZerodhaPin As Label
End Class
