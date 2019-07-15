Imports System.Threading
Imports System.IO

Public Class frmATMSettings
    Private _cts As CancellationTokenSource = Nothing
    Private _ATMSettings As ATMUserInputs = Nothing
    Private _ATMSettingsFilename As String = Path.Combine(My.Application.Info.DirectoryPath, "ATMSettings.Strategy.a2t")

    Public Sub New(ByRef userInputs As ATMUserInputs)
        InitializeComponent()
        _ATMSettings = userInputs
    End Sub

    Private Sub frmCandleRangeBreakoutSettings_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadSettings()
    End Sub

    Private Sub btnCandleRangeBreakoutSettings_Click(sender As Object, e As EventArgs) Handles btnCandleRangeBreakoutSettings.Click
        Try
            _cts = New CancellationTokenSource
            If _ATMSettings Is Nothing Then _ATMSettings = New ATMUserInputs
            _ATMSettings.InstrumentsData = Nothing
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
        If File.Exists(_ATMSettingsFilename) Then
            _ATMSettings = Utilities.Strings.DeserializeToCollection(Of ATMUserInputs)(_ATMSettingsFilename)
            txtSignalTimeFrame.Text = _ATMSettings.SignalTimeFrame
            dtpckrTradeStartTime.Value = _ATMSettings.TradeStartTime
            dtpckrLastTradeEntryTime.Value = _ATMSettings.LastTradeEntryTime
            dtpckrEODExitTime.Value = _ATMSettings.EODExitTime
            txtMaxLossPerDay.Text = _ATMSettings.MaxLossPerDay
            txtMaxProfitPerDay.Text = _ATMSettings.MaxProfitPerDay
            txtTelegramAPI.Text = _ATMSettings.TelegramAPIKey
            txtTelegramChatID.Text = _ATMSettings.TelegramChatID
            txtTelegramChatIDForPL.Text = _ATMSettings.TelegramPLChatID
            txtInstrumentDetalis.Text = _ATMSettings.InstrumentDetailsFilePath
        End If
    End Sub

    Private Sub SaveSettings()
        _ATMSettings.SignalTimeFrame = txtSignalTimeFrame.Text
        _ATMSettings.TradeStartTime = dtpckrTradeStartTime.Value
        _ATMSettings.LastTradeEntryTime = dtpckrLastTradeEntryTime.Value
        _ATMSettings.EODExitTime = dtpckrEODExitTime.Value
        _ATMSettings.MaxLossPerDay = txtMaxLossPerDay.Text
        _ATMSettings.MaxProfitPerDay = txtMaxProfitPerDay.Text
        _ATMSettings.TelegramAPIKey = txtTelegramAPI.Text
        _ATMSettings.TelegramChatID = txtTelegramChatID.Text
        _ATMSettings.TelegramPLChatID = txtTelegramChatIDForPL.Text
        _ATMSettings.InstrumentDetailsFilePath = txtInstrumentDetalis.Text

        Utilities.Strings.SerializeFromCollection(Of ATMUserInputs)(_ATMSettingsFilename, _ATMSettings)
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
        _ATMSettings.FillInstrumentDetails(txtInstrumentDetalis.Text, _cts)
    End Sub

    Private Sub ValidateInputs()
        ValidateNumbers(1, 60, txtSignalTimeFrame)
        ValidateNumbers(Decimal.MinValue, Decimal.MaxValue, txtMaxLossPerDay)
        ValidateNumbers(0, Decimal.MaxValue, txtMaxProfitPerDay)
        ValidateFile()
    End Sub
End Class