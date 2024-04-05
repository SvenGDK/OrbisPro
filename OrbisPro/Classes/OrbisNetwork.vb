Imports ManagedNativeWifi
Imports OrbisPro.OrbisStructures
Imports OrbisPro.ROOT.CIMV2.Win32
Imports System.IO
Imports System.Management

Public Class OrbisNetwork

#Region "WiFi"

    Public Shared Function GetConnectedWiFiNetworkSSID() As NetworkIdentifier
        Dim ListOfConnectedWiFiNetworks As New List(Of NetworkIdentifier)
        For Each ConnectedWiFiNetwork As NetworkIdentifier In NativeWifi.EnumerateConnectedNetworkSsids()
            ListOfConnectedWiFiNetworks.Add(ConnectedWiFiNetwork)
        Next
        If ListOfConnectedWiFiNetworks IsNot Nothing AndAlso ListOfConnectedWiFiNetworks.Count > 0 Then
            Return ListOfConnectedWiFiNetworks(0)
        Else
            Return Nothing
        End If
    End Function

    Public Shared Function CreateWiFiProfile(NetworkName As String, NetworkNameInHex As String,
                                             ConnectionType As String, ConnectionMode As String,
                                             Authentication As String, Encryption As String, UseOneX As String,
                                             KeyType As String, IsProtected As String, KeyMaterial As String) As String

        Dim WiFiXMLProfile As XDocument = <?xml version="1.0"?>
                                          <WLANProfile xmlns="http://www.microsoft.com/networking/WLAN/profile/v1">
                                              <name><%= NetworkName %></name>
                                              <SSIDConfig>
                                                  <SSID>
                                                      <hex><%= NetworkNameInHex %></hex>
                                                      <name><%= NetworkName %></name>
                                                  </SSID>
                                              </SSIDConfig>
                                              <connectionType><%= ConnectionType %></connectionType>
                                              <connectionMode><%= ConnectionMode %></connectionMode>
                                              <MSM>
                                                  <security>
                                                      <authEncryption>
                                                          <authentication><%= Authentication %></authentication>
                                                          <encryption><%= Encryption %></encryption>
                                                          <useOneX><%= UseOneX %></useOneX>
                                                      </authEncryption>
                                                      <sharedKey>
                                                          <keyType><%= KeyType %></keyType>
                                                          <protected><%= IsProtected %></protected>
                                                          <keyMaterial><%= KeyMaterial %></keyMaterial>
                                                      </sharedKey>
                                                  </security>
                                              </MSM>
                                          </WLANProfile>

        WiFiXMLProfile.Save(FileIO.FileSystem.CurrentDirectory + "\System\WiFi\" + NetworkName + ".xml")

        If File.Exists(FileIO.FileSystem.CurrentDirectory + "\System\WiFi\" + NetworkName + ".xml") Then
            Return FileIO.FileSystem.CurrentDirectory + "\System\WiFi\" + NetworkName + ".xml"
        Else
            Return Nothing
        End If
    End Function

    Public Shared Function CreateOpenWiFiProfile(NetworkName As String, NetworkNameInHex As String, ConnectionType As String, ConnectionMode As String, Authentication As String, Encryption As String, UseOneX As String) As String
        Dim WiFiXMLProfile As XDocument = <?xml version="1.0"?>
                                          <WLANProfile xmlns="http://www.microsoft.com/networking/WLAN/profile/v1">
                                              <name><%= NetworkName %></name>
                                              <SSIDConfig>
                                                  <SSID>
                                                      <hex><%= NetworkNameInHex %></hex>
                                                      <name><%= NetworkName %></name>
                                                  </SSID>
                                              </SSIDConfig>
                                              <connectionType><%= ConnectionType %></connectionType>
                                              <connectionMode><%= ConnectionMode %></connectionMode>
                                              <MSM>
                                                  <security>
                                                      <authEncryption>
                                                          <authentication><%= Authentication %></authentication>
                                                          <encryption><%= Encryption %></encryption>
                                                          <useOneX><%= UseOneX %></useOneX>
                                                      </authEncryption>
                                                  </security>
                                              </MSM>
                                          </WLANProfile>

        WiFiXMLProfile.Save(FileIO.FileSystem.CurrentDirectory + "\System\WiFi\" + NetworkName + ".xml")

        If File.Exists(FileIO.FileSystem.CurrentDirectory + "\System\WiFi\" + NetworkName + ".xml") Then
            Return FileIO.FileSystem.CurrentDirectory + "\System\WiFi\" + NetworkName + ".xml"
        Else
            Return Nothing
        End If
    End Function

    Public Shared Function IsWiFiRadioOn() As Boolean
        Dim WiFiInterfaces As IEnumerable(Of InterfaceInfo) = NativeWifi.EnumerateInterfaces()

        If WiFiInterfaces IsNot Nothing Then
            Dim FirstWiFiInterface As InterfaceInfo = WiFiInterfaces(0)
            Dim WiFiInterfaceRadioSet As RadioSet = NativeWifi.GetInterfaceRadio(FirstWiFiInterface.Id).RadioSets(0)

            If WiFiInterfaceRadioSet IsNot Nothing Then
                If WiFiInterfaceRadioSet.SoftwareOn Then
                    Return True
                Else
                    Return False
                End If
            Else
                Return False
            End If
        Else
            Return False
        End If
    End Function

    Public Shared Function TurnWiFiOff(WiFiInterface As InterfaceInfo) As Boolean
        If NativeWifi.TurnOffInterfaceRadio(WiFiInterface.Id) Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Shared Function TurnWiFiOn(WiFiInterface As InterfaceInfo) As Boolean
        If NativeWifi.TurnOnInterfaceRadio(WiFiInterface.Id) Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Shared Function GetWiFiSignalStrenght() As Integer
        Dim ConnectedWifi As NetworkIdentifier = GetConnectedWiFiNetworkSSID()
        If ConnectedWifi IsNot Nothing Then
            Dim AvailableWiFiNetworks As IEnumerable(Of AvailableNetworkPack) = NativeWifi.EnumerateAvailableNetworks()
            If AvailableWiFiNetworks IsNot Nothing Then
                Dim SignalQualityStrenght As Integer = 0
                For Each AvailableWiFiNetwork As AvailableNetworkPack In AvailableWiFiNetworks
                    'If the connected SSID matches exactly a found SSID then return it's SignalQuality
                    If AvailableWiFiNetwork.Ssid.ToString() = ConnectedWifi.ToString() Then
                        SignalQualityStrenght = AvailableWiFiNetwork.SignalQuality
                        Exit For
                    End If
                Next
                Return SignalQualityStrenght
            Else
                Return 0
            End If
        Else
            Return 0
        End If
    End Function

    Public Shared Function GetWiFiSignalImage(SignalQualityStrenght As Integer) As ImageSource
        Dim TempBitmapImage = New BitmapImage()

        TempBitmapImage.BeginInit()
        TempBitmapImage.CacheOption = BitmapCacheOption.OnLoad
        TempBitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache

        If SignalQualityStrenght >= 80 Then
            TempBitmapImage.UriSource = New Uri("pack://application:,,,/Icons/Wifi/WiFiHigh.png", UriKind.RelativeOrAbsolute)
        ElseIf SignalQualityStrenght >= 40 Then
            TempBitmapImage.UriSource = New Uri("pack://application:,,,/Icons/Wifi/WiFiMid.png", UriKind.RelativeOrAbsolute)
        ElseIf SignalQualityStrenght >= 20 Then
            TempBitmapImage.UriSource = New Uri("pack://application:,,,/Icons/Wifi/WiFiLow.png", UriKind.RelativeOrAbsolute)
        ElseIf SignalQualityStrenght >= 1 Then
            TempBitmapImage.UriSource = New Uri("pack://application:,,,/Icons/Wifi/WiFiOff.png", UriKind.RelativeOrAbsolute)
        End If

        TempBitmapImage.EndInit()

        Return TempBitmapImage
    End Function

#End Region

#Region "Ethernet"

    Public Shared Function GetActiveEthernetDevices() As List(Of EthernetDevice)
        Dim NewListOfEthernetDevices As New List(Of EthernetDevice)
        Dim NewQuery As New SelectQuery("Win32_NetworkAdapter", "NetConnectionStatus=2")
        Dim NetworkAdapterSeacher As New ManagementObjectSearcher(NewQuery)

        For Each FoundNetworkAdapter As ManagementObject In NetworkAdapterSeacher.Get()
            Dim NewNetworkAdapter As New NetworkAdapter(FoundNetworkAdapter)
            Dim NewEthernetDevice As New EthernetDevice() With {
                .AdapterType = NewNetworkAdapter.AdapterType,
                .Description = NewNetworkAdapter.Description,
                .DeviceID = NewNetworkAdapter.DeviceID,
                .MACAddress = NewNetworkAdapter.MACAddress,
                .Manufacturer = NewNetworkAdapter.Manufacturer,
                .Name = NewNetworkAdapter.Name,
                .NetEnabled = NewNetworkAdapter.NetEnabled,
                .Speed = NewNetworkAdapter.Speed,
                .NetworkAddresses = NewNetworkAdapter.NetworkAddresses}

            NewListOfEthernetDevices.Add(NewEthernetDevice)
        Next

        Return NewListOfEthernetDevices
    End Function

    Public Shared Function GetDisabledEthernetDevices() As List(Of EthernetDevice)
        Dim NewListOfEthernetDevices As New List(Of EthernetDevice)
        Dim NewQuery As New SelectQuery("Win32_NetworkAdapter", "NetConnectionStatus=0")
        Dim NetworkAdapterSeacher As New ManagementObjectSearcher(NewQuery)

        For Each FoundNetworkAdapter As ManagementObject In NetworkAdapterSeacher.Get()
            Dim NewNetworkAdapter As New NetworkAdapter(FoundNetworkAdapter)
            Dim NewEthernetDevice As New EthernetDevice() With {
                .AdapterType = NewNetworkAdapter.AdapterType,
                .Description = NewNetworkAdapter.Description,
                .DeviceID = NewNetworkAdapter.DeviceID,
                .MACAddress = NewNetworkAdapter.MACAddress,
                .Manufacturer = NewNetworkAdapter.Manufacturer,
                .Name = NewNetworkAdapter.Name,
                .NetEnabled = NewNetworkAdapter.NetEnabled,
                .Speed = NewNetworkAdapter.Speed,
                .NetworkAddresses = NewNetworkAdapter.NetworkAddresses}

            NewListOfEthernetDevices.Add(NewEthernetDevice)
        Next

        Return NewListOfEthernetDevices
    End Function

    Public Shared Function DisableEthernetDevice(EthernetAdapterName As String, Optional DisableAll As Boolean = False) As Boolean
        Dim NewQuery As New SelectQuery("Win32_NetworkAdapter", "NetConnectionStatus=2")
        Dim NetworkAdapterSeacher As New ManagementObjectSearcher(NewQuery)
        Dim DidDisable As Boolean = False

        For Each FoundNetworkAdapter As ManagementObject In NetworkAdapterSeacher.Get()
            Dim NewNetworkAdapter As New NetworkAdapter(FoundNetworkAdapter)

            If DisableAll Then
                If NewNetworkAdapter.NetEnabled Then
                    If NewNetworkAdapter.Disable() = 0 Then
                        DidDisable = True
                    End If
                End If
            Else
                If NewNetworkAdapter.Name = EthernetAdapterName AndAlso NewNetworkAdapter.NetEnabled Then
                    If NewNetworkAdapter.Disable() = 0 Then
                        DidDisable = True
                        Exit For
                    Else
                        DidDisable = False
                        Exit For
                    End If
                End If
            End If
        Next

        If DidDisable Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Shared Function EnableEthernetDevice(EthernetAdapterName As String, Optional EnableAll As Boolean = False) As Boolean
        Dim NewQuery As New SelectQuery("Win32_NetworkAdapter", "NetConnectionStatus=0")
        Dim NetworkAdapterSeacher As New ManagementObjectSearcher(NewQuery)
        Dim DidEnable As Boolean = False

        For Each FoundNetworkAdapter As ManagementObject In NetworkAdapterSeacher.Get()
            Dim NewNetworkAdapter As New NetworkAdapter(FoundNetworkAdapter)

            If EnableAll Then
                If NewNetworkAdapter.NetEnabled = False Then
                    If NewNetworkAdapter.Enable() = 0 Then
                        DidEnable = True
                    End If
                End If
            Else
                If NewNetworkAdapter.Name = EthernetAdapterName AndAlso NewNetworkAdapter.NetEnabled = False Then
                    If NewNetworkAdapter.Enable() = 0 Then
                        DidEnable = True
                        Exit For
                    Else
                        DidEnable = False
                        Exit For
                    End If
                End If
            End If
        Next

        If DidEnable Then
            Return True
        Else
            Return False
        End If
    End Function

#End Region

End Class
