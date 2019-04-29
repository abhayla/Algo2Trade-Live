Imports System.IO
Imports System.Threading

Public Class frmOHLSettings

    Private _cts As CancellationTokenSource = Nothing
    Private _OHLSettings As OHLUserInputs = Nothing
    Private _OHLSettingsFilename As String = Path.Combine(My.Application.Info.DirectoryPath, "OHLSettings.Strategy.a2t")

    Public Sub New(ByRef MRUserInputs As OHLUserInputs)
        InitializeComponent()
        _OHLSettings = MRUserInputs
    End Sub

    Private Sub frmOHLSettings_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadSettings()
    End Sub

    Private Sub btnSaveOHLSettings_Click(sender As Object, e As EventArgs) Handles btnSaveOHLSettings.Click
        Try
            _cts = New CancellationTokenSource
            If _OHLSettings Is Nothing Then _OHLSettings = New OHLUserInputs
            _OHLSettings.InstrumentsData = Nothing
            ValidateInputs()
            SaveSettings()
            Me.Close()
        Catch ex As Exception
            MsgBox(String.Format("The following error occurred: {0}", ex.Message), MsgBoxStyle.Critical)
        End Try
    End Sub

    Private Sub LoadSettings()
        If File.Exists(_OHLSettingsFilename) Then
            _OHLSettings = Utilities.Strings.DeserializeToCollection(Of OHLUserInputs)(_OHLSettingsFilename)
            dtpckrTradeStartTime.Value = _OHLSettings.TradeStartTime
            dtpckrLastTradeEntryTime.Value = _OHLSettings.LastTradeEntryTime
            dtpckrEODExitTime.Value = _OHLSettings.EODExitTime
            txtMaxLossPercentagePerDay.Text = _OHLSettings.MaxLossPercentagePerDay
            txtMaxProfitPercentagePerDay.Text = _OHLSettings.MaxProfitPercentagePerDay
            txtStoplossPercentage.Text = _OHLSettings.StoplossPercentage
            txtTargetPercentage.Text = _OHLSettings.TargetPercentage
            txtInstrumentDetalis.Text = _OHLSettings.InstrumentDetailsFilePath
        End If
    End Sub

    Private Sub SaveSettings()
        _OHLSettings.TradeStartTime = dtpckrTradeStartTime.Value
        _OHLSettings.LastTradeEntryTime = dtpckrLastTradeEntryTime.Value
        _OHLSettings.EODExitTime = dtpckrEODExitTime.Value
        _OHLSettings.MaxLossPercentagePerDay = txtMaxLossPercentagePerDay.Text
        _OHLSettings.MaxProfitPercentagePerDay = txtMaxProfitPercentagePerDay.Text
        _OHLSettings.StoplossPercentage = txtStoplossPercentage.Text
        _OHLSettings.TargetPercentage = txtTargetPercentage.Text
        _OHLSettings.InstrumentDetailsFilePath = txtInstrumentDetalis.Text

        Utilities.Strings.SerializeFromCollection(Of OHLUserInputs)(_OHLSettingsFilename, _OHLSettings)
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
        _OHLSettings.FillInstrumentDetails(txtInstrumentDetalis.Text, _cts)
    End Sub
    Private Sub ValidateInputs()
        ValidateNumbers(0, 100, txtMaxLossPercentagePerDay)
        ValidateNumbers(0, 100, txtMaxProfitPercentagePerDay)
        ValidateNumbers(0, 100, txtStoplossPercentage)
        ValidateNumbers(0, 100, txtTargetPercentage)
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