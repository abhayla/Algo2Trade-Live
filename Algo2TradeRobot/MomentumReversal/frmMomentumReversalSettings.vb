Imports System.IO
Imports System.Threading

Public Class frmMomentumReversalSettings

    Private _cts As CancellationTokenSource = Nothing
    Private _MRSettings As MomentumReversalUserInputs = Nothing
    Private _MRSettingsFilename As String = Path.Combine(My.Application.Info.DirectoryPath, "MomentumReversalSettings.Strategy.a2t")

    Public Sub New(ByRef MRUserInputs As MomentumReversalUserInputs)
        InitializeComponent()
        _MRSettings = MRUserInputs
    End Sub

    Private Sub frmMomentumReversalSettings_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadSettings()
    End Sub
    Private Sub btnSaveMomentumReversalSettings_Click(sender As Object, e As EventArgs) Handles btnSaveMomentumReversalSettings.Click
        Try
            _cts = New CancellationTokenSource
            If _MRSettings Is Nothing Then _MRSettings = New MomentumReversalUserInputs
            _MRSettings.InstrumentsData = Nothing
            ValidateInputs()
            SaveSettings()
            Me.Close()
        Catch ex As Exception
            MsgBox(String.Format("The following error occurred: {0}", ex.Message), MsgBoxStyle.Critical)
        End Try
    End Sub
    Private Sub LoadSettings()
        If File.Exists(_MRSettingsFilename) Then
            _MRSettings = Utilities.Strings.DeserializeToCollection(Of MomentumReversalUserInputs)(_MRSettingsFilename)
            txtSignalTimeFrame.Text = _MRSettings.SignalTimeFrame
            dtpckrTradeStartTime.Value = _MRSettings.TradeStartTime
            dtpckrLastTradeEntryTime.Value = _MRSettings.LastTradeEntryTime
            dtpckrEODExitTime.Value = _MRSettings.EODExitTime
            dtpckrIdleTimeStart.Value = _MRSettings.IdleTimeStart
            dtpckrIdleTimeEnd.Value = _MRSettings.IdleTimeEnd
            txtTradeOpenTime.Text = _MRSettings.TradeOpenTime
            txtInstrumentDetalis.Text = _MRSettings.InstrumentDetailsFilePath

            txtRSIPeriod.Text = _MRSettings.RSIPeriod
            txtRSILevel.Text = _MRSettings.RSILevel
        End If
    End Sub
    Private Sub SaveSettings()
        _MRSettings.SignalTimeFrame = txtSignalTimeFrame.Text
        _MRSettings.TradeStartTime = dtpckrTradeStartTime.Value
        _MRSettings.LastTradeEntryTime = dtpckrLastTradeEntryTime.Value
        _MRSettings.EODExitTime = dtpckrEODExitTime.Value
        _MRSettings.IdleTimeStart = dtpckrIdleTimeStart.Value
        _MRSettings.IdleTimeEnd = dtpckrIdleTimeEnd.Value
        _MRSettings.TradeOpenTime = txtTradeOpenTime.Text
        _MRSettings.InstrumentDetailsFilePath = txtInstrumentDetalis.Text

        _MRSettings.RSIPeriod = txtRSIPeriod.Text
        _MRSettings.RSILevel = txtRSILevel.Text

        Utilities.Strings.SerializeFromCollection(Of MomentumReversalUserInputs)(_MRSettingsFilename, _MRSettings)
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
        _MRSettings.FillInstrumentDetails(txtInstrumentDetalis.Text, _cts)
    End Sub
    Private Sub ValidateInputs()
        ValidateNumbers(1, 60, txtSignalTimeFrame, True)
        ValidateNumbers(1, 60, txtTradeOpenTime, True)
        ValidateNumbers(1, Integer.MaxValue, txtRSIPeriod, True)
        ValidateNumbers(1, Integer.MaxValue, txtRSILevel, True)
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