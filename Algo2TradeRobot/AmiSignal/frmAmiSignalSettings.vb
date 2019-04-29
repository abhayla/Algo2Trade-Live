Imports System.IO
Imports System.Threading

Public Class frmAmiSignalSettings

    Private _cts As CancellationTokenSource = Nothing
    Private _AmiSignalSettings As AmiSignalUserInputs = Nothing
    Private _AmiSignalSettingsFilename As String = Path.Combine(My.Application.Info.DirectoryPath, "AmiSignalSettings.Strategy.a2t")

    Public Sub New(ByRef userInputs As AmiSignalUserInputs)
        InitializeComponent()
        _AmiSignalSettings = userInputs
    End Sub

    Private Sub frmAmiSignalSettings_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadSettings()
    End Sub

    Private Sub btnSaveAmiSignalSettings_Click(sender As Object, e As EventArgs) Handles btnSaveAmiSignalSettings.Click
        Try
            _cts = New CancellationTokenSource
            If _AmiSignalSettings Is Nothing Then _AmiSignalSettings = New AmiSignalUserInputs
            _AmiSignalSettings.InstrumentsData = Nothing
            ValidateInputs()
            SaveSettings()
            Me.Close()
        Catch ex As Exception
            MsgBox(String.Format("The following error occurred: {0}", ex.Message), MsgBoxStyle.Critical)
        End Try
    End Sub

    Private Sub LoadSettings()
        If File.Exists(_AmiSignalSettingsFilename) Then
            _AmiSignalSettings = Utilities.Strings.DeserializeToCollection(Of AmiSignalUserInputs)(_AmiSignalSettingsFilename)
            txtMaxOpenPositions.Text = _AmiSignalSettings.MaxNumberOfOpenPositions
            dtpckrTradeStartTime.Value = _AmiSignalSettings.TradeStartTime
            dtpckrLastTradeEntryTime.Value = _AmiSignalSettings.LastTradeEntryTime
            dtpckrEODExitTime.Value = _AmiSignalSettings.EODExitTime
            txtInstrumentDetalis.Text = _AmiSignalSettings.InstrumentDetailsFilePath
        End If
    End Sub

    Private Sub SaveSettings()
        _AmiSignalSettings.MaxNumberOfOpenPositions = txtMaxOpenPositions.Text
        _AmiSignalSettings.TradeStartTime = dtpckrTradeStartTime.Value
        _AmiSignalSettings.LastTradeEntryTime = dtpckrLastTradeEntryTime.Value
        _AmiSignalSettings.EODExitTime = dtpckrEODExitTime.Value
        _AmiSignalSettings.InstrumentDetailsFilePath = txtInstrumentDetalis.Text

        Utilities.Strings.SerializeFromCollection(Of AmiSignalUserInputs)(_AmiSignalSettingsFilename, _AmiSignalSettings)
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
        _AmiSignalSettings.FillInstrumentDetails(txtInstrumentDetalis.Text, _cts)
    End Sub

    Private Sub ValidateInputs()
        ValidateNumbers(1, Integer.MaxValue, txtMaxOpenPositions)
        ValidateFile()
    End Sub

    Private Sub btnBrowse_Click(sender As Object, e As EventArgs) Handles btnBrowse.Click
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
End Class