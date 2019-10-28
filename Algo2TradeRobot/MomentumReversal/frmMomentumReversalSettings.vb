Imports System.IO
Imports System.Threading

Public Class frmMomentumReversalSettings

    Private _cts As CancellationTokenSource = Nothing
    Private _settings As MomentumReversalUserInputs = Nothing
    Private _MRSettingsFilename As String = Path.Combine(My.Application.Info.DirectoryPath, "MomentumReversalSettings.Strategy.a2t")

    Public Sub New(ByRef MRUserInputs As MomentumReversalUserInputs)
        InitializeComponent()
        _settings = MRUserInputs
    End Sub

    Private Sub frmMomentumReversalSettings_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadSettings()
    End Sub
    Private Sub btnSaveMomentumReversalSettings_Click(sender As Object, e As EventArgs) Handles btnSaveMomentumReversalSettings.Click
        Try
            _cts = New CancellationTokenSource
            If _settings Is Nothing Then _settings = New MomentumReversalUserInputs
            _settings.InstrumentsData = Nothing
            ValidateInputs()
            SaveSettings()
            Me.Close()
        Catch ex As Exception
            MsgBox(String.Format("The following error occurred: {0}", ex.Message), MsgBoxStyle.Critical)
        End Try
    End Sub
    Private Sub LoadSettings()
        If File.Exists(_MRSettingsFilename) Then
            _settings = Utilities.Strings.DeserializeToCollection(Of MomentumReversalUserInputs)(_MRSettingsFilename)
            txtSignalTimeFrame.Text = _settings.SignalTimeFrame
            dtpckrTradeStartTime.Value = _settings.TradeStartTime
            dtpckrLastTradeEntryTime.Value = _settings.LastTradeEntryTime
            dtpckrEODExitTime.Value = _settings.EODExitTime
            txtMaxLossPerDay.Text = _settings.MaxLossPerDay
            txtMaxProfitPerDay.Text = _settings.MaxProfitPerDay
            txtATRPeriod.Text = _settings.ATRPeriod
            txtNumberOfTradePerStock.Text = _settings.NumberOfTradePerStock
            txtTargetMultiplier.Text = _settings.TargetMultiplier
            txtInstrumentDetalis.Text = _settings.InstrumentDetailsFilePath

            chbAutoSelectStock.Checked = _settings.AutoSelectStock

            txtMinPrice.Text = _settings.MinPrice
            txtMaxPrice.Text = _settings.MaxPrice
            txtATRPercentage.Text = _settings.ATRPercentage
            txtMinVolume.Text = _settings.MinVolume
            txtBlankCandlePercentage.Text = _settings.BlankCandlePercentage
            txtNumberOfStock.Text = _settings.NumberOfStock

            txtTelegramAPI.Text = _settings.TelegramAPIKey
            txtTelegramChatID.Text = _settings.TelegramChatID
            txtTelegramChatIDForPL.Text = _settings.TelegramPLChatID
        End If
    End Sub
    Private Sub SaveSettings()
        _settings.SignalTimeFrame = txtSignalTimeFrame.Text
        _settings.TradeStartTime = dtpckrTradeStartTime.Value
        _settings.LastTradeEntryTime = dtpckrLastTradeEntryTime.Value
        _settings.EODExitTime = dtpckrEODExitTime.Value
        _settings.MaxLossPerDay = txtMaxLossPerDay.Text
        _settings.MaxProfitPerDay = txtMaxProfitPerDay.Text
        _settings.ATRPeriod = txtATRPeriod.Text
        _settings.NumberOfTradePerStock = txtNumberOfTradePerStock.Text
        _settings.TargetMultiplier = txtTargetMultiplier.Text
        _settings.InstrumentDetailsFilePath = txtInstrumentDetalis.Text

        _settings.AutoSelectStock = chbAutoSelectStock.Checked

        _settings.MinPrice = txtMinPrice.Text
        _settings.MaxPrice = txtMaxPrice.Text
        _settings.ATRPercentage = txtATRPercentage.Text
        _settings.MinVolume = txtMinVolume.Text
        _settings.BlankCandlePercentage = txtBlankCandlePercentage.Text
        _settings.NumberOfStock = txtNumberOfStock.Text

        _settings.TelegramAPIKey = txtTelegramAPI.Text
        _settings.TelegramChatID = txtTelegramChatID.Text
        _settings.TelegramPLChatID = txtTelegramChatIDForPL.Text

        Utilities.Strings.SerializeFromCollection(Of MomentumReversalUserInputs)(_MRSettingsFilename, _settings)
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
        _settings.FillInstrumentDetails(txtInstrumentDetalis.Text, _cts)
    End Sub
    Private Sub ValidateInputs()
        ValidateNumbers(0, 999, txtTargetMultiplier)
        ValidateNumbers(1, 60, txtSignalTimeFrame)
        ValidateFile()
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
End Class