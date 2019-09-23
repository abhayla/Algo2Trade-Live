Imports System.Threading
Imports System.IO

Public Class frmLowSLSettings
    Private _cts As CancellationTokenSource = Nothing
    Private _LowSLSettings As LowSLUserInputs = Nothing
    Private _LowSLSettingsFilename As String = Path.Combine(My.Application.Info.DirectoryPath, "LowSLSettings.Strategy.a2t")

    Public Sub New(ByRef userInputs As LowSLUserInputs)
        InitializeComponent()
        _LowSLSettings = userInputs
    End Sub

    Private Sub frmLowSLSettings_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadSettings()
    End Sub

    Private Sub btnLowSLSettings_Click(sender As Object, e As EventArgs) Handles btnLowSLStrategySettings.Click
        Try
            _cts = New CancellationTokenSource
            If _LowSLSettings Is Nothing Then _LowSLSettings = New LowSLUserInputs
            _LowSLSettings.InstrumentsData = Nothing
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
        If File.Exists(_LowSLSettingsFilename) Then
            _LowSLSettings = Utilities.Strings.DeserializeToCollection(Of LowSLUserInputs)(_LowSLSettingsFilename)
            txtATRPeriod.Text = _LowSLSettings.ATRPeriod
            txtSignalTimeFrame.Text = _LowSLSettings.SignalTimeFrame
            dtpckrTradeStartTime.Value = _LowSLSettings.TradeStartTime
            dtpckrLastTradeEntryTime.Value = _LowSLSettings.LastTradeEntryTime
            dtpckrEODExitTime.Value = _LowSLSettings.EODExitTime
            txtTargetMultiplier.Text = _LowSLSettings.TargetMultiplier
            txtNumberOfTradePerStock.Text = _LowSLSettings.NumberOfTradePerStock
            txtStockMaxLossPerDay.Text = _LowSLSettings.StockMaxLossPerDay
            txtStockMaxProfitPerDay.Text = _LowSLSettings.StockMaxProfitPerDay
            txtMaxLossPerDay.Text = _LowSLSettings.MaxLossPerDay
            txtMaxProfitPerDay.Text = _LowSLSettings.MaxProfitPerDay
            txtInstrumentDetalis.Text = _LowSLSettings.InstrumentDetailsFilePath

            chbAutoSelectStock.Checked = _LowSLSettings.AutoSelectStock
            rdbCash.Checked = _LowSLSettings.CashInstrument
            rdbFuture.Checked = _LowSLSettings.FutureInstrument
            txtMaxStoploss.Text = _LowSLSettings.MaxStoploss
            txtMinCapital.Text = _LowSLSettings.MinCapital

            txtMinPrice.Text = _LowSLSettings.MinPrice
            txtMaxPrice.Text = _LowSLSettings.MaxPrice
            txtATRPercentage.Text = _LowSLSettings.ATRPercentage
            txtMinVolume.Text = _LowSLSettings.MinVolume
            txtNumberOfStock.Text = _LowSLSettings.NumberOfStock
            txtMinVolumeSpikePer.Text = _LowSLSettings.MinVolumeSpikePercentage
            txtMaxCapital.Text = _LowSLSettings.MaxCapital
        End If
    End Sub

    Private Sub SaveSettings()
        _LowSLSettings.ATRPeriod = txtATRPeriod.Text
        _LowSLSettings.SignalTimeFrame = txtSignalTimeFrame.Text
        _LowSLSettings.TradeStartTime = dtpckrTradeStartTime.Value
        _LowSLSettings.LastTradeEntryTime = dtpckrLastTradeEntryTime.Value
        _LowSLSettings.EODExitTime = dtpckrEODExitTime.Value
        _LowSLSettings.TargetMultiplier = txtTargetMultiplier.Text
        _LowSLSettings.NumberOfTradePerStock = txtNumberOfTradePerStock.Text
        _LowSLSettings.StockMaxLossPerDay = txtStockMaxLossPerDay.Text
        _LowSLSettings.StockMaxProfitPerDay = txtStockMaxProfitPerDay.Text
        _LowSLSettings.MaxLossPerDay = txtMaxLossPerDay.Text
        _LowSLSettings.MaxProfitPerDay = txtMaxProfitPerDay.Text
        _LowSLSettings.InstrumentDetailsFilePath = txtInstrumentDetalis.Text

        _LowSLSettings.AutoSelectStock = chbAutoSelectStock.Checked
        _LowSLSettings.CashInstrument = rdbCash.Checked
        _LowSLSettings.FutureInstrument = rdbFuture.Checked
        _LowSLSettings.MaxStoploss = txtMaxStoploss.Text
        _LowSLSettings.MinCapital = txtMinCapital.Text

        _LowSLSettings.MinPrice = txtMinPrice.Text
        _LowSLSettings.MaxPrice = txtMaxPrice.Text
        _LowSLSettings.ATRPercentage = txtATRPercentage.Text
        _LowSLSettings.MinVolume = txtMinVolume.Text
        _LowSLSettings.NumberOfStock = txtNumberOfStock.Text
        _LowSLSettings.MinVolumeSpikePercentage = txtMinVolumeSpikePer.Text
        _LowSLSettings.MaxCapital = txtMaxCapital.Text

        Utilities.Strings.SerializeFromCollection(Of LowSLUserInputs)(_LowSLSettingsFilename, _LowSLSettings)
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
        _LowSLSettings.FillInstrumentDetails(txtInstrumentDetalis.Text, _cts)
    End Sub

    Private Sub ValidateInputs()
        ValidateNumbers(1, 60, txtSignalTimeFrame)
        ValidateNumbers(1, 100, txtATRPeriod)
        ValidateFile()
    End Sub
End Class