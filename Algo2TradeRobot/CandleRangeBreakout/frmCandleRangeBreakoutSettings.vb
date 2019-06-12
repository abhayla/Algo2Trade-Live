Imports System.Threading
Imports System.IO

Public Class frmCandleRangeBreakoutSettings
    Private _cts As CancellationTokenSource = Nothing
    Private _CandleRangeBreakoutSettings As CandleRangeBreakoutUserInputs = Nothing
    Private _CandleRangeBreakoutSettingsFilename As String = Path.Combine(My.Application.Info.DirectoryPath, "CandleRangeBreakoutSettings.Strategy.a2t")

    Public Sub New(ByRef userInputs As CandleRangeBreakoutUserInputs)
        InitializeComponent()
        _CandleRangeBreakoutSettings = userInputs
    End Sub

    Private Sub frmCandleRangeBreakoutSettings_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadSettings()
    End Sub

    Private Sub btnCandleRangeBreakoutSettings_Click(sender As Object, e As EventArgs) Handles btnCandleRangeBreakoutSettings.Click
        Try
            _cts = New CancellationTokenSource
            If _CandleRangeBreakoutSettings Is Nothing Then _CandleRangeBreakoutSettings = New CandleRangeBreakoutUserInputs
            _CandleRangeBreakoutSettings.InstrumentsData = Nothing
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
        If File.Exists(_CandleRangeBreakoutSettingsFilename) Then
            _CandleRangeBreakoutSettings = Utilities.Strings.DeserializeToCollection(Of CandleRangeBreakoutUserInputs)(_CandleRangeBreakoutSettingsFilename)
            txtSignalTimeFrame.Text = _CandleRangeBreakoutSettings.SignalTimeFrame
            dtpckrTradeStartTime.Value = _CandleRangeBreakoutSettings.TradeStartTime
            dtpckrLastTradeEntryTime.Value = _CandleRangeBreakoutSettings.LastTradeEntryTime
            dtpckrEODExitTime.Value = _CandleRangeBreakoutSettings.EODExitTime
            txtMaxLossPerDay.Text = _CandleRangeBreakoutSettings.MaxLossPerDay
            txtMaxProfitPerDay.Text = _CandleRangeBreakoutSettings.MaxProfitPerDay
            txtTelegramAPI.Text = _CandleRangeBreakoutSettings.TelegramAPIKey
            txtTelegramChatID.Text = _CandleRangeBreakoutSettings.TelegramChatID
            txtTelegramChatIDForPL.Text = _CandleRangeBreakoutSettings.TelegramPLChatID
            txtInstrumentDetalis.Text = _CandleRangeBreakoutSettings.InstrumentDetailsFilePath
        End If
    End Sub

    Private Sub SaveSettings()
        _CandleRangeBreakoutSettings.SignalTimeFrame = txtSignalTimeFrame.Text
        _CandleRangeBreakoutSettings.TradeStartTime = dtpckrTradeStartTime.Value
        _CandleRangeBreakoutSettings.LastTradeEntryTime = dtpckrLastTradeEntryTime.Value
        _CandleRangeBreakoutSettings.EODExitTime = dtpckrEODExitTime.Value
        _CandleRangeBreakoutSettings.MaxLossPerDay = txtMaxLossPerDay.Text
        _CandleRangeBreakoutSettings.MaxProfitPerDay = txtMaxProfitPerDay.Text
        _CandleRangeBreakoutSettings.TelegramAPIKey = txtTelegramAPI.Text
        _CandleRangeBreakoutSettings.TelegramChatID = txtTelegramChatID.Text
        _CandleRangeBreakoutSettings.TelegramPLChatID = txtTelegramChatIDForPL.Text
        _CandleRangeBreakoutSettings.InstrumentDetailsFilePath = txtInstrumentDetalis.Text

        Utilities.Strings.SerializeFromCollection(Of CandleRangeBreakoutUserInputs)(_CandleRangeBreakoutSettingsFilename, _CandleRangeBreakoutSettings)
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
        _CandleRangeBreakoutSettings.FillInstrumentDetails(txtInstrumentDetalis.Text, _cts)
    End Sub

    Private Sub ValidateInputs()
        ValidateNumbers(1, 60, txtSignalTimeFrame)
        ValidateNumbers(Decimal.MinValue, Decimal.MaxValue, txtMaxLossPerDay)
        ValidateNumbers(0, Decimal.MaxValue, txtMaxProfitPerDay)
        ValidateFile()
    End Sub
End Class