Imports Algo2TradeCore.Entities.UserSettings
Imports Algo2TradeCore.Entities

Public Class frmAdvancedOptions

    Private _UserInputs As ControllerUserInputs

    Public Sub New(ByVal userInputs As ControllerUserInputs)
        InitializeComponent()
        Me._UserInputs = userInputs
    End Sub

    Private Sub frmAdvancedOptions_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadSettings()
    End Sub

    Private Sub btnSaveDelaySettings_Click(sender As Object, e As EventArgs) Handles btnSaveDelaySettings.Click
        Try
            ValidateInputs()
            SaveSettings()
            Me.Close()
        Catch ex As Exception
            MsgBox(String.Format("The following error occurred: {0}", ex.Message), MsgBoxStyle.Critical)
        End Try
    End Sub

    Private Sub LoadSettings()
        If _UserInputs IsNot Nothing Then
            txtGetInformationDelay.Text = _UserInputs.GetInformationDelay
            txtBackToBackOrderCoolOffDelay.Text = _UserInputs.BackToBackOrderCoolOffDelay
            dtpckrForceRestartTime.Value = _UserInputs.ForceRestartTime
            If _UserInputs.ExchangeDetails IsNot Nothing Then
                dtpckrNSEExchangeStartTime.Value = _UserInputs.ExchangeDetails("NSE").ExchangeStartTime
                dtpckrNSEExchangeEndTime.Value = _UserInputs.ExchangeDetails("NSE").ExchangeEndTime
                dtpckrMCXExchangeStartTime.Value = _UserInputs.ExchangeDetails("MCX").ExchangeStartTime
                dtpckrMCXExchangeEndTime.Value = _UserInputs.ExchangeDetails("MCX").ExchangeEndTime
                dtpckrCDSExchangeStartTime.Value = _UserInputs.ExchangeDetails("CDS").ExchangeStartTime
                dtpckrCDSExchangeEndTime.Value = _UserInputs.ExchangeDetails("CDS").ExchangeEndTime
            End If
        End If
    End Sub

    Private Sub SaveSettings()
        If _UserInputs Is Nothing Then _UserInputs = New ControllerUserInputs
        _UserInputs.GetInformationDelay = txtGetInformationDelay.Text
        _UserInputs.BackToBackOrderCoolOffDelay = txtBackToBackOrderCoolOffDelay.Text
        _UserInputs.ForceRestartTime = dtpckrForceRestartTime.Value
        _UserInputs.ExchangeDetails = New Dictionary(Of String, Exchange) From {
            {"NSE", New Exchange(Enums.TypeOfExchage.NSE) With
            {.ExchangeStartTime = dtpckrNSEExchangeStartTime.Value, .ExchangeEndTime = dtpckrNSEExchangeEndTime.Value}},
            {"NFO", New Exchange(Enums.TypeOfExchage.NSE) With
            {.ExchangeStartTime = dtpckrNSEExchangeStartTime.Value, .ExchangeEndTime = dtpckrNSEExchangeEndTime.Value}},
            {"MCX", New Exchange(Enums.TypeOfExchage.MCX) With
            {.ExchangeStartTime = dtpckrMCXExchangeStartTime.Value, .ExchangeEndTime = dtpckrMCXExchangeEndTime.Value}},
            {"CDS", New Exchange(Enums.TypeOfExchage.CDS) With
            {.ExchangeStartTime = dtpckrCDSExchangeStartTime.Value, .ExchangeEndTime = dtpckrCDSExchangeEndTime.Value}}
        }
        Utilities.Strings.SerializeFromCollection(Of ControllerUserInputs)(ControllerUserInputs.Filename, _UserInputs)
    End Sub

    Private Sub ValidateInputs()
        ValidateNumbers(1, 1000, txtGetInformationDelay)
        ValidateNumbers(1, 1000, txtBackToBackOrderCoolOffDelay)
        If dtpckrForceRestartTime.Value.Hour = 0 AndAlso dtpckrForceRestartTime.Value.Minute = 0 Then
            Throw New ApplicationException("Force Restart Time can not be blank")
        End If
    End Sub

    Private Function ValidateNumbers(ByVal startNumber As Integer, ByVal endNumber As Integer, ByVal inputTB As TextBox) As Boolean
        Dim ret As Boolean = False
        If IsNumeric(inputTB.Text) Then
            If Val(inputTB.Text) >= startNumber And Val(inputTB.Text) <= endNumber Then
                ret = True
            End If
        End If
        If Not ret Then Throw New ApplicationException(String.Format("{0} cannot have a value < {1} or > {2}", inputTB.Tag, startNumber, endNumber))
        Return ret
    End Function

End Class