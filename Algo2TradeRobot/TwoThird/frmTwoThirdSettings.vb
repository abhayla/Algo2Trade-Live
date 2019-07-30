Imports System.Threading
Imports System.IO

Public Class frmTwoThirdSettings
    Private _cts As CancellationTokenSource = Nothing
    Private _settings As TwoThirdUserInputs = Nothing
    Private _settingsFilename As String = Path.Combine(My.Application.Info.DirectoryPath, "TwoThirdSettings.Strategy.a2t")

    Public Sub New(ByRef userInputs As TwoThirdUserInputs)
        InitializeComponent()
        _settings = userInputs
    End Sub

    Private Sub frmTwoThirdSettings_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadSettings()
        chbStoplossMoveToBreakeven_CheckedChanged(sender, e)
    End Sub

    Private Sub btnTwoThirdStrayegySettings_Click(sender As Object, e As EventArgs) Handles btnTwoThirdStrayegySettings.Click
        Try
            _cts = New CancellationTokenSource
            If _settings Is Nothing Then _settings = New TwoThirdUserInputs
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

    Private Sub chbStoplossMoveToBreakeven_CheckedChanged(sender As Object, e As EventArgs) Handles chbStoplossMoveToBreakeven.CheckedChanged
        If chbStoplossMoveToBreakeven.Checked Then
            chbCountTradesWithBreakevenMovement.Enabled = True
        Else
            chbCountTradesWithBreakevenMovement.Checked = False
            chbCountTradesWithBreakevenMovement.Enabled = False
        End If
    End Sub

    Private Sub LoadSettings()
        If File.Exists(_settingsFilename) Then
            _settings = Utilities.Strings.DeserializeToCollection(Of TwoThirdUserInputs)(_settingsFilename)
            txtATRPeriod.Text = _settings.ATRPeriod
            txtSignalTimeFrame.Text = _settings.SignalTimeFrame
            dtpckrTradeStartTime.Value = _settings.TradeStartTime
            dtpckrLastTradeEntryTime.Value = _settings.LastTradeEntryTime
            dtpckrEODExitTime.Value = _settings.EODExitTime
            txtNumberOfTradePerStock.Text = _settings.NumberOfTradePerStock
            txtTargetMultiplier.Text = _settings.TargetMultiplier
            txtMaxLossPerDay.Text = _settings.MaxLossPerDay
            txtMaxProfitPerDay.Text = _settings.MaxProfitPerDay
            chbReverseTrade.Checked = _settings.ReverseTrade
            chbStoplossMoveToBreakeven.Checked = _settings.StoplossMovementToBreakeven
            chbCountTradesWithBreakevenMovement.Checked = _settings.CountTradesWithBreakevenMovement
            chbAutoSelectStock.Checked = _settings.AutoSelectStock
            chbCash.Checked = _settings.CashInstrument
            chbFuture.Checked = _settings.FutureInstrument
            chbAllowToIncreaseCapital.Checked = _settings.AllowToIncreaseCapital
            txtFutureMinCapital.Text = _settings.MinCapital
            txtManualStockList.Text = _settings.ManualInstrumentList
            txtMinPrice.Text = _settings.MinPrice
            txtMaxPrice.Text = _settings.MaxPrice
            txtATRPercentage.Text = _settings.ATRPercentage
            txtMinVolume.Text = _settings.MinVolume
            txtNumberOfStock.Text = _settings.NumberOfStock
            txtInstrumentDetalis.Text = _settings.InstrumentDetailsFilePath
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
        _settings.MaxLossPerDay = txtMaxLossPerDay.Text
        _settings.MaxProfitPerDay = txtMaxProfitPerDay.Text
        _settings.ReverseTrade = chbReverseTrade.Checked
        _settings.StoplossMovementToBreakeven = chbStoplossMoveToBreakeven.Checked
        _settings.CountTradesWithBreakevenMovement = chbCountTradesWithBreakevenMovement.Checked
        _settings.AutoSelectStock = chbAutoSelectStock.Checked
        _settings.CashInstrument = chbCash.Checked
        _settings.FutureInstrument = chbFuture.Checked
        _settings.AllowToIncreaseCapital = chbAllowToIncreaseCapital.Checked
        _settings.MinCapital = txtFutureMinCapital.Text
        _settings.ManualInstrumentList = txtManualStockList.Text
        _settings.MinPrice = txtMinPrice.Text
        _settings.MaxPrice = txtMaxPrice.Text
        _settings.ATRPercentage = txtATRPercentage.Text
        _settings.MinVolume = txtMinVolume.Text
        _settings.NumberOfStock = txtNumberOfStock.Text
        _settings.InstrumentDetailsFilePath = txtInstrumentDetalis.Text

        Utilities.Strings.SerializeFromCollection(Of TwoThirdUserInputs)(_settingsFilename, _settings)
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
        ValidateNumbers(1, Decimal.MaxValue, txtATRPeriod)
        ValidateNumbers(1, 60, txtSignalTimeFrame)
        ValidateNumbers(Decimal.MinValue, Decimal.MaxValue, txtNumberOfTradePerStock)
        ValidateNumbers(0, Decimal.MaxValue, txtTargetMultiplier)
        ValidateFile()
    End Sub
End Class