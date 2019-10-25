Imports System.Threading
Imports System.IO

Public Class frmPetDGandhiSettings
    Private _cts As CancellationTokenSource = Nothing
    Private _settings As PetDGandhiUserInputs = Nothing
    Private _PetDGandhiSettingsFilename As String = Path.Combine(My.Application.Info.DirectoryPath, "PetDGandhiSettings.Strategy.a2t")

    Public Sub New(ByRef PetDGandhiUserInputs As PetDGandhiUserInputs)
        InitializeComponent()
        _settings = PetDGandhiUserInputs
    End Sub

    Private Sub frmPetDGandhiSettings_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadSettings()
    End Sub

    Private Sub btnSavePetDGandhiSettings_Click(sender As Object, e As EventArgs) Handles btnSavePetDGandhiSettings.Click
        Try
            _cts = New CancellationTokenSource
            If _settings Is Nothing Then _settings = New PetDGandhiUserInputs
            _settings.InstrumentsData = Nothing
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
        If File.Exists(_PetDGandhiSettingsFilename) Then
            _settings = Utilities.Strings.DeserializeToCollection(Of PetDGandhiUserInputs)(_PetDGandhiSettingsFilename)
            txtATRPeriod.Text = _settings.ATRPeriod
            txtSignalTimeFrame.Text = _settings.SignalTimeFrame
            dtpckrTradeStartTime.Value = _settings.TradeStartTime
            dtpckrLastTradeEntryTime.Value = _settings.LastTradeEntryTime
            dtpckrEODExitTime.Value = _settings.EODExitTime
            txtNumberOfTradePerStock.Text = _settings.NumberOfTradePerStock
            txtTargetMultiplier.Text = _settings.TargetMultiplier
            txtMaxLossPerDay.Text = _settings.MaxLossPerDay
            txtMaxProfitPerDay.Text = _settings.MaxProfitPerDay
            txtInstrumentDetalis.Text = _settings.InstrumentDetailsFilePath
            txtPinbarTalePercentage.Text = _settings.PinbarTalePercentage
            txtMaxLossPerStockMultiplier.Text = _settings.MaxLossPerStockMultiplier
            txtMaxProfitPerStockMultiplier.Text = _settings.MaxProfitPerStockMultiplier
            txtMaxLossPerTradeMultiplier.Text = _settings.MaxLossPerTradeMultiplier
            txtMinLossPercentagePerTrade.Text = _settings.MinLossPercentagePerTrade

            chbAutoSelectStock.Checked = _settings.AutoSelectStock
            chbCash.Checked = _settings.CashInstrument
            chbFuture.Checked = _settings.FutureInstrument
            chbAllowToIncreaseCapital.Checked = _settings.AllowToIncreaseCapital
            txtMinCapital.Text = _settings.MinCapital

            txtMinPrice.Text = _settings.MinPrice
            txtMaxPrice.Text = _settings.MaxPrice
            txtATRPercentage.Text = _settings.ATRPercentage
            txtMinVolume.Text = _settings.MinVolume
            txtBlankCandlePercentage.Text = _settings.BlankCandlePercentage
            txtNumberOfStock.Text = _settings.NumberOfStock

            txtTelegramAPI.Text = _settings.TelegramAPIKey
            txtTelegramTradeChatID.Text = _settings.TelegramTradeChatID
            txtTelegramSignalChatID.Text = _settings.TelegramSignalChatID
            txtTelegramTargetChatID.Text = _settings.TelegramTargetChatID
            txtTelegramMTMChatID.Text = _settings.TelegramMTMChatID
        End If
    End Sub
    Private Sub SaveSettings()
        _settings.ATRPeriod = txtATRPeriod.Text
        _settings.SignalTimeFrame = txtSignalTimeFrame.Text
        _settings.TradeStartTime = dtpckrTradeStartTime.Value
        _settings.LastTradeEntryTime = dtpckrLastTradeEntryTime.Value
        _settings.EODExitTime = dtpckrEODExitTime.Value
        _settings.NumberOfTradePerStock = txtNumberOfTradePerStock.Text
        _settings.TargetMultiplier = txtTargetMultiplier.Text
        _settings.MaxLossPerDay = Math.Abs(CDec(txtMaxLossPerDay.Text))
        _settings.MaxProfitPerDay = txtMaxProfitPerDay.Text
        _settings.InstrumentDetailsFilePath = txtInstrumentDetalis.Text
        _settings.PinbarTalePercentage = txtPinbarTalePercentage.Text
        _settings.MaxLossPerStockMultiplier = Math.Abs(CDec(txtMaxLossPerStockMultiplier.Text))
        _settings.MaxProfitPerStockMultiplier = txtMaxProfitPerStockMultiplier.Text
        _settings.MaxLossPerTradeMultiplier = Math.Abs(CDec(txtMaxLossPerTradeMultiplier.Text))
        _settings.MinLossPercentagePerTrade = Math.Abs(CDec(txtMinLossPercentagePerTrade.Text))

        _settings.AutoSelectStock = chbAutoSelectStock.Checked
        _settings.CashInstrument = chbCash.Checked
        _settings.FutureInstrument = chbFuture.Checked
        _settings.AllowToIncreaseCapital = chbAllowToIncreaseCapital.Checked
        _settings.MinCapital = txtMinCapital.Text

        _settings.MinPrice = txtMinPrice.Text
        _settings.MaxPrice = txtMaxPrice.Text
        _settings.ATRPercentage = txtATRPercentage.Text
        _settings.MinVolume = txtMinVolume.Text
        _settings.BlankCandlePercentage = txtBlankCandlePercentage.Text
        _settings.NumberOfStock = txtNumberOfStock.Text

        _settings.TelegramAPIKey = txtTelegramAPI.Text
        _settings.TelegramTradeChatID = txtTelegramTradeChatID.Text
        _settings.TelegramSignalChatID = txtTelegramSignalChatID.Text
        _settings.TelegramTargetChatID = txtTelegramTargetChatID.Text
        _settings.TelegramMTMChatID = txtTelegramMTMChatID.Text

        Utilities.Strings.SerializeFromCollection(Of PetDGandhiUserInputs)(_PetDGandhiSettingsFilename, _settings)
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
        ValidateNumbers(1, 60, txtSignalTimeFrame)
        ValidateFile()
    End Sub

End Class