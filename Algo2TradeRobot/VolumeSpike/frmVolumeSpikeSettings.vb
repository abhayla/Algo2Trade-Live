Imports System.Threading
Imports System.IO

Public Class frmVolumeSpikeSettings
    Private _cts As CancellationTokenSource = Nothing
    Private _VolumeSpikeSettings As VolumeSpikeUserInputs = Nothing
    Private _VolumeSpikeSettingsFilename As String = Path.Combine(My.Application.Info.DirectoryPath, "VolumeSpikeSettings.Strategy.a2t")

    Public Sub New(ByRef userInputs As VolumeSpikeUserInputs)
        InitializeComponent()
        _VolumeSpikeSettings = userInputs
    End Sub

    Private Sub frmCandleRangeBreakoutSettings_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadSettings()
    End Sub

    Private Sub btnVolumeSpikeSettings_Click(sender As Object, e As EventArgs) Handles btnVolumeSpikeStrategySettings.Click
        Try
            _cts = New CancellationTokenSource
            If _VolumeSpikeSettings Is Nothing Then _VolumeSpikeSettings = New VolumeSpikeUserInputs
            _VolumeSpikeSettings.InstrumentsData = Nothing
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
        If File.Exists(_VolumeSpikeSettingsFilename) Then
            _VolumeSpikeSettings = Utilities.Strings.DeserializeToCollection(Of VolumeSpikeUserInputs)(_VolumeSpikeSettingsFilename)
            txtATRPeriod.Text = _VolumeSpikeSettings.ATRPeriod
            txtSignalTimeFrame.Text = _VolumeSpikeSettings.SignalTimeFrame
            dtpckrTradeStartTime.Value = _VolumeSpikeSettings.TradeStartTime
            dtpckrLastTradeEntryTime.Value = _VolumeSpikeSettings.LastTradeEntryTime
            dtpckrEODExitTime.Value = _VolumeSpikeSettings.EODExitTime
            txtTargetMultiplier.Text = _VolumeSpikeSettings.TargetMultiplier
            txtNumberOfTradePerStock.Text = _VolumeSpikeSettings.NumberOfTradePerStock
            txtMaxLossPerDay.Text = _VolumeSpikeSettings.MaxLossPerDay
            txtMaxProfitPerDay.Text = _VolumeSpikeSettings.MaxProfitPerDay
            txtInstrumentDetalis.Text = _VolumeSpikeSettings.InstrumentDetailsFilePath
            chbAutoSelectStock.Checked = _VolumeSpikeSettings.AutoSelectStock
            rdbCash.Checked = _VolumeSpikeSettings.CashInstrument
            rdbFuture.Checked = _VolumeSpikeSettings.FutureInstrument
            txtMinCapital.Text = _VolumeSpikeSettings.MinCapital
            txtManualStockList.Text = _VolumeSpikeSettings.ManualInstrumentList
            txtMinPrice.Text = _VolumeSpikeSettings.MinPrice
            txtMaxPrice.Text = _VolumeSpikeSettings.MaxPrice
            txtATRPercentage.Text = _VolumeSpikeSettings.ATRPercentage
            txtMinVolume.Text = _VolumeSpikeSettings.MinVolume
            txtNumberOfStock.Text = _VolumeSpikeSettings.NumberOfStock
        End If
    End Sub

    Private Sub SaveSettings()
        _VolumeSpikeSettings.ATRPeriod = txtATRPeriod.Text
        _VolumeSpikeSettings.SignalTimeFrame = txtSignalTimeFrame.Text
        _VolumeSpikeSettings.TradeStartTime = dtpckrTradeStartTime.Value
        _VolumeSpikeSettings.LastTradeEntryTime = dtpckrLastTradeEntryTime.Value
        _VolumeSpikeSettings.EODExitTime = dtpckrEODExitTime.Value
        _VolumeSpikeSettings.TargetMultiplier = txtTargetMultiplier.Text
        _VolumeSpikeSettings.NumberOfTradePerStock = txtNumberOfTradePerStock.Text
        _VolumeSpikeSettings.MaxLossPerDay = txtMaxLossPerDay.Text
        _VolumeSpikeSettings.MaxProfitPerDay = txtMaxProfitPerDay.Text
        _VolumeSpikeSettings.InstrumentDetailsFilePath = txtInstrumentDetalis.Text
        _VolumeSpikeSettings.AutoSelectStock = chbAutoSelectStock.Checked
        _VolumeSpikeSettings.CashInstrument = rdbCash.Checked
        _VolumeSpikeSettings.FutureInstrument = rdbFuture.Checked
        _VolumeSpikeSettings.MinCapital = txtMinCapital.Text
        _VolumeSpikeSettings.ManualInstrumentList = txtManualStockList.Text
        _VolumeSpikeSettings.MinPrice = txtMinPrice.Text
        _VolumeSpikeSettings.MaxPrice = txtMaxPrice.Text
        _VolumeSpikeSettings.ATRPercentage = txtATRPercentage.Text
        _VolumeSpikeSettings.MinVolume = txtMinVolume.Text
        _VolumeSpikeSettings.NumberOfStock = txtNumberOfStock.Text

        Utilities.Strings.SerializeFromCollection(Of VolumeSpikeUserInputs)(_VolumeSpikeSettingsFilename, _VolumeSpikeSettings)
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
        _VolumeSpikeSettings.FillInstrumentDetails(txtInstrumentDetalis.Text, _cts)
    End Sub

    Private Sub ValidateInputs()
        ValidateNumbers(1, 60, txtSignalTimeFrame)
        ValidateNumbers(1, 100, txtATRPeriod)
        ValidateNumbers(1, 100, txtTargetMultiplier)
        ValidateNumbers(0, Decimal.MaxValue, txtMinCapital)
        ValidateFile()
    End Sub

End Class