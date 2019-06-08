Imports System.IO
Imports System.Threading

Public Class frmEMACrossoverSettings

    Private _cts As CancellationTokenSource = Nothing
    Private _EMACrossoverSettings As EMACrossoverUserInputs = Nothing
    Private _EMACrossoverSettingsFilename As String = Path.Combine(My.Application.Info.DirectoryPath, "EMACrossoverSettings.Strategy.a2t")

    Public Sub New(ByRef emaCrossoverUserInput As EMACrossoverUserInputs)
        InitializeComponent()
        _EMACrossoverSettings = emaCrossoverUserInput
    End Sub

    Private Sub frmEMACrossoverSettings_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadSettings()
    End Sub

    Private Sub btnSaveEMACrossoverSettings_Click(sender As Object, e As EventArgs) Handles btnSaveEMACrossoverSettings.Click
        Try
            _cts = New CancellationTokenSource
            If _EMACrossoverSettings Is Nothing Then _EMACrossoverSettings = New EMACrossoverUserInputs
            _EMACrossoverSettings.InstrumentsData = Nothing
            ValidateInputs()
            SaveSettings()
            Me.Close()
        Catch ex As Exception
            MsgBox(String.Format("The following error occurred: {0}", ex.Message), MsgBoxStyle.Critical)
        End Try
    End Sub

    Private Sub btnBrowse_Click(sender As Object, e As EventArgs) Handles btnBrowse.Click
        opnFileSettings.Filter = "|*.csv"
        opnFileSettings.ShowDialog()
    End Sub

    Private Sub opnFileSettings_FileOk(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles opnFileSettings.FileOk
        Dim extension As String = Path.GetExtension(opnFileSettings.FileName)
        If extension = ".csv" Then
            txtInstrumentDetalis.Text = opnFileSettings.FileName
        Else
            MsgBox("File Type not supported. Please Try again.", MsgBoxStyle.Critical)
        End If
    End Sub

    Private Sub LoadSettings()
        If File.Exists(_EMACrossoverSettingsFilename) Then
            _EMACrossoverSettings = Utilities.Strings.DeserializeToCollection(Of EMACrossoverUserInputs)(_EMACrossoverSettingsFilename)
            txtSignalTimeFrame.Text = _EMACrossoverSettings.SignalTimeFrame
            txtFastEMAPeriod.Text = _EMACrossoverSettings.FastEMAPeriod
            txtSlowEMAPeriod.Text = _EMACrossoverSettings.SlowEMAPeriod
            txtInstrumentDetalis.Text = _EMACrossoverSettings.InstrumentDetailsFilePath
        End If
    End Sub
    Private Sub SaveSettings()
        _EMACrossoverSettings.SignalTimeFrame = txtSignalTimeFrame.Text
        _EMACrossoverSettings.FastEMAPeriod = txtFastEMAPeriod.Text
        _EMACrossoverSettings.SlowEMAPeriod = txtSlowEMAPeriod.Text
        _EMACrossoverSettings.InstrumentDetailsFilePath = txtInstrumentDetalis.Text

        Utilities.Strings.SerializeFromCollection(Of EMACrossoverUserInputs)(_EMACrossoverSettingsFilename, _EMACrossoverSettings)
    End Sub

    Private Function ValidateNumbers(ByVal startNumber As Decimal, ByVal endNumber As Decimal, ByVal inputTB As TextBox) As Boolean
        Dim ret As Boolean = False
        If IsNumeric(inputTB.Text) Then
            If Val(inputTB.Text) >= startNumber And Val(inputTB.Text) <= endNumber Then
                ret = True
            End If
        End If
        If Not ret Then Throw New ApplicationException(String.Format("{0} cannot have a value < {1} or > {2}", inputTB.Tag, startNumber, endNumber))
        Return ret
    End Function

    Private Sub ValidateFile()
        _EMACrossoverSettings.FillInstrumentDetails(txtInstrumentDetalis.Text, _cts)
    End Sub

    Private Sub ValidateInputs()
        ValidateNumbers(1, 60, txtSignalTimeFrame)
        ValidateNumbers(1, Integer.MaxValue, txtSlowEMAPeriod)
        ValidateNumbers(1, Integer.MaxValue, txtFastEMAPeriod)
        ValidateFile()
    End Sub
End Class