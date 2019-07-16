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

    Private Sub btnATMSettings_Click(sender As Object, e As EventArgs) Handles btnATMStrategySettings.Click
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
            txtATRPeriod.Text = _ATMSettings.ATRPeriod
            txtSignalTimeFrame.Text = _ATMSettings.SignalTimeFrame
            dtpckrTradeStartTime.Value = _ATMSettings.TradeStartTime
            dtpckrLastTradeEntryTime.Value = _ATMSettings.LastTradeEntryTime
            dtpckrEODExitTime.Value = _ATMSettings.EODExitTime
            txtTargetMultiplier.Text = _ATMSettings.TargetMultiplier
            txtInstrumentDetalis.Text = _ATMSettings.InstrumentDetailsFilePath
            chbCash.Checked = _ATMSettings.CashInstrument
            chbFuture.Checked = _ATMSettings.FutureInstrument
            txtCashMaxSL.Text = _ATMSettings.CashMaxSL
            txtFutureMinCapital.Text = _ATMSettings.FutureMinCapital
            txtManualStockList.Text = _ATMSettings.ManualInstrumentList
        End If
    End Sub

    Private Sub SaveSettings()
        _ATMSettings.ATRPeriod = txtATRPeriod.Text
        _ATMSettings.SignalTimeFrame = txtSignalTimeFrame.Text
        _ATMSettings.TradeStartTime = dtpckrTradeStartTime.Value
        _ATMSettings.LastTradeEntryTime = dtpckrLastTradeEntryTime.Value
        _ATMSettings.EODExitTime = dtpckrEODExitTime.Value
        _ATMSettings.TargetMultiplier = txtTargetMultiplier.Text
        _ATMSettings.InstrumentDetailsFilePath = txtInstrumentDetalis.Text
        _ATMSettings.CashInstrument = chbCash.Checked
        _ATMSettings.FutureInstrument = chbFuture.Checked
        _ATMSettings.CashMaxSL = txtCashMaxSL.Text
        _ATMSettings.FutureMinCapital = txtFutureMinCapital.Text
        _ATMSettings.ManualInstrumentList = txtManualStockList.Text

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
        ValidateNumbers(1, 100, txtATRPeriod)
        ValidateNumbers(0, Decimal.MaxValue, txtCashMaxSL)
        ValidateNumbers(0, Decimal.MaxValue, txtFutureMinCapital)
        ValidateFile()
    End Sub

End Class