Imports System.IO
Imports System.Threading

Public Class frmMomentumReversalSettings

    Private _cts As CancellationTokenSource = Nothing
    Private _settings As MomentumReversalUserInputs = Nothing
    Private _settingsFilename As String = Path.Combine(My.Application.Info.DirectoryPath, "Algo2Trade.Strategy.a2t")

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
        If File.Exists(_settingsFilename) Then
            _settings = Utilities.Strings.DeserializeToCollection(Of MomentumReversalUserInputs)(_settingsFilename)
            txtSignalTimeFrame.Text = _settings.SignalTimeFrame
            dtpckrTradeStartTime.Value = _settings.TradeStartTime
            dtpckrLastTradeEntryTime.Value = _settings.LastTradeEntryTime
            dtpckrEODExitTime.Value = _settings.EODExitTime
            txtNumberOfTradePerStock.Text = _settings.NumberOfTradePerStock
            txtMinStoploss.Text = _settings.MinStoplossPercentage
            txtMinTarget.Text = _settings.MinTargetPercentage
            txtCostToCostMovement.Text = _settings.CostToCostMovementPercentage
            txtInstrumentDetalis.Text = _settings.InstrumentDetailsFilePath

            txtATRPeriod.Text = _settings.ATRPeriod
        End If
    End Sub
    Private Sub SaveSettings()
        _settings.SignalTimeFrame = txtSignalTimeFrame.Text
        _settings.TradeStartTime = dtpckrTradeStartTime.Value
        _settings.LastTradeEntryTime = dtpckrLastTradeEntryTime.Value
        _settings.EODExitTime = dtpckrEODExitTime.Value
        _settings.NumberOfTradePerStock = txtNumberOfTradePerStock.Text
        _settings.MinStoplossPercentage = txtMinStoploss.Text
        _settings.MinTargetPercentage = txtMinTarget.Text
        _settings.CostToCostMovementPercentage = txtCostToCostMovement.Text
        _settings.InstrumentDetailsFilePath = txtInstrumentDetalis.Text

        _settings.ATRPeriod = txtATRPeriod.Text

        Utilities.Strings.SerializeFromCollection(Of MomentumReversalUserInputs)(_settingsFilename, _settings)
    End Sub
    Private Function ValidateNumbers(ByVal startNumber As Decimal, ByVal endNumber As Decimal, ByVal inputTB As TextBox, Optional ByVal validateInteger As Boolean = False) As Boolean
        Dim ret As Boolean = False
        If IsNumeric(inputTB.Text) Then
            If validateInteger Then
                If Val(inputTB.Text) <> Math.Round(Val(inputTB.Text), 0) Then
                    Throw New ApplicationException(String.Format("{0} should be of type Integer", inputTB.Tag))
                End If
            End If
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
        ValidateNumbers(1, 60, txtSignalTimeFrame, True)
        ValidateNumbers(1, Integer.MaxValue, txtATRPeriod, True)
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