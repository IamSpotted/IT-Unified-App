# Settings Page UI Example - Domain Configuration Section

## XAML Code for Settings Page

Add this section to your Settings page XAML to provide a user-friendly domain configuration interface:

```xml
<!-- Authorization Settings Section -->
<StackLayout Margin="10">
    <Label Text="Authorization Settings" 
           FontSize="18" 
           FontAttributes="Bold" 
           Margin="0,10,0,5" />
    
    <!-- Current User Information -->
    <Frame BackgroundColor="{AppThemeBinding Light=#F5F5F5, Dark=#2C2C2C}" 
           Padding="15" 
           Margin="0,5">
        <StackLayout>
            <Label Text="Current User Information" 
                   FontSize="16" 
                   FontAttributes="Bold" 
                   Margin="0,0,0,10" />
            
            <Grid RowDefinitions="Auto,Auto,Auto" 
                  ColumnDefinitions="Auto,*">
                
                <Label Grid.Row="0" Grid.Column="0" 
                       Text="User: " 
                       FontAttributes="Bold" />
                <Label Grid.Row="0" Grid.Column="1" 
                       Text="{Binding CurrentUserName}" />
                
                <Label Grid.Row="1" Grid.Column="0" 
                       Text="Role: " 
                       FontAttributes="Bold" />
                <Label Grid.Row="1" Grid.Column="1" 
                       Text="{Binding CurrentUserRole}" />
                
                <Label Grid.Row="2" Grid.Column="0" 
                       Text="Domain: " 
                       FontAttributes="Bold" />
                <Label Grid.Row="2" Grid.Column="1" 
                       Text="{Binding DomainName}" />
            </Grid>
        </StackLayout>
    </Frame>
    
    <!-- Domain Configuration Toggle -->
    <Button Text="{Binding ShowDomainConfiguration, Converter={StaticResource BoolToTextConverter}, ConverterParameter='Hide Domain Settings|Configure Domain Settings'}"
            Command="{Binding ToggleDomainConfigurationCommand}"
            BackgroundColor="{AppThemeBinding Light=#E3F2FD, Dark=#1565C0}"
            Margin="0,10" />
    
    <!-- Security Notice for Non-SystemAdmin Users -->
    <Frame IsVisible="{Binding ShowDomainConfiguration}"
           BackgroundColor="{AppThemeBinding Light=#FFF3E0, Dark=#E65100}"
           Padding="10"
           Margin="0,5"
           IsVisible="{Binding CanEditDomainConfiguration, Converter={StaticResource InvertedBoolConverter}}">
        <StackLayout>
            <Label Text="🔒 Domain Configuration Restricted" 
                   FontAttributes="Bold"
                   TextColor="{AppThemeBinding Light=#E65100, Dark=#FFB74D}" />
            <Label Text="Only System Administrators can modify domain settings. Current role: "
                   TextColor="{AppThemeBinding Light=#E65100, Dark=#FFB74D}" />
            <Label Text="{Binding CurrentUserRole}"
                   FontAttributes="Bold"
                   TextColor="{AppThemeBinding Light=#E65100, Dark=#FFB74D}" />
        </StackLayout>
    </Frame>
    
    <!-- Domain Configuration Panel (Collapsible) -->
    <StackLayout IsVisible="{Binding ShowDomainConfiguration}" 
                 Margin="10,0">
        
        <Frame BackgroundColor="{AppThemeBinding Light=#FFF9C4, Dark=#F57F17}" 
               Padding="15" 
               Margin="0,5">
            <StackLayout>
                <Label Text="⚙️ Domain Configuration" 
                       FontSize="16" 
                       FontAttributes="Bold" 
                       Margin="0,0,0,10" />
                
                <!-- Domain Name -->
                <Label Text="Domain Name:" FontAttributes="Bold" />
                <Entry Text="{Binding DomainName}" 
                       Placeholder="e.g., COMPANY.LOCAL"
                       IsReadOnly="{Binding CanEditDomainConfiguration, Converter={StaticResource InvertedBoolConverter}}"
                       Margin="0,0,0,10" />
                
                <!-- AD Group Names -->
                <Label Text="Active Directory Groups:" 
                       FontAttributes="Bold" 
                       Margin="0,10,0,5" />
                
                <Label Text="ReadOnly Group:" />
                <Entry Text="{Binding ReadOnlyGroupName}" 
                       Placeholder="e.g., ITSF-App-ReadOnly"
                       IsReadOnly="{Binding CanEditDomainConfiguration, Converter={StaticResource InvertedBoolConverter}}"
                       Margin="0,0,0,5" />
                
                <Label Text="Standard Group:" />
                <Entry Text="{Binding StandardGroupName}" 
                       Placeholder="e.g., ITSF-App-Standard"
                       IsReadOnly="{Binding CanEditDomainConfiguration, Converter={StaticResource InvertedBoolConverter}}"
                       Margin="0,0,0,5" />
                
                <Label Text="Database Admin Group:" />
                <Entry Text="{Binding DatabaseAdminGroupName}" 
                       Placeholder="e.g., ITSF-App-DatabaseAdmin"
                       IsReadOnly="{Binding CanEditDomainConfiguration, Converter={StaticResource InvertedBoolConverter}}"
                       Margin="0,0,0,5" />
                
                <Label Text="System Admin Group:" />
                <Entry Text="{Binding SystemAdminGroupName}" 
                       Placeholder="e.g., ITSF-App-SystemAdmin"
                       IsReadOnly="{Binding CanEditDomainConfiguration, Converter={StaticResource InvertedBoolConverter}}"
                       Margin="0,0,0,10" />
                
                <!-- Action Buttons (Only visible to SystemAdmin) -->
                <Grid ColumnDefinitions="*,*" 
                      ColumnSpacing="10" 
                      Margin="0,10,0,0"
                      IsVisible="{Binding CanEditDomainConfiguration}">
                    
                    <Button Grid.Column="0"
                            Text="💾 Save Configuration" 
                            Command="{Binding SaveDomainConfigurationCommand}"
                            BackgroundColor="{AppThemeBinding Light=#4CAF50, Dark=#2E7D32}" 
                            TextColor="White" />
                    
                    <Button Grid.Column="1"
                            Text="🔌 Test Connection" 
                            Command="{Binding TestDomainConnectionCommand}"
                            BackgroundColor="{AppThemeBinding Light=#2196F3, Dark=#1565C0}" 
                            TextColor="White" />
                </Grid>
            </StackLayout>
        </Frame>
    </StackLayout>
    
    <!-- Developer Mode Section (if enabled) -->
    <StackLayout IsVisible="{Binding DeveloperModeEnabled}" 
                 Margin="0,20,0,0">
        
        <Frame BackgroundColor="{AppThemeBinding Light=#FFEBEE, Dark=#C62828}" 
               Padding="15">
            <StackLayout>
                <Label Text="🛠️ Developer Mode Active" 
                       FontSize="16" 
                       FontAttributes="Bold" 
                       TextColor="{AppThemeBinding Light=#D32F2F, Dark=#FFCDD2}" 
                       Margin="0,0,0,10" />
                
                <Label Text="Simulated Role:" 
                       FontAttributes="Bold" />
                <Picker ItemsSource="{Binding SimulatedRoleOptions}"
                        SelectedItem="{Binding SimulatedRole}"
                        Title="Select Role to Simulate" />
                
                <Button Text="Apply Role Simulation" 
                        Command="{Binding ChangeSimulatedRoleCommand}"
                        BackgroundColor="{AppThemeBinding Light=#FF5722, Dark=#BF360C}" 
                        TextColor="White" 
                        Margin="0,10,0,0" />
            </StackLayout>
        </Frame>
    </StackLayout>
</StackLayout>
```

## Converter Needed

You'll need this converter for the toggle button text (if not already present):

```csharp
public class BoolToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string paramText)
        {
            var options = paramText.Split('|');
            if (options.Length == 2)
            {
                return boolValue ? options[0] : options[1];
            }
        }
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

## Key Features

1. **User Information Display**: Shows current user, role, and domain
2. **Collapsible Configuration**: Domain settings can be hidden/shown
3. **🔒 Security Restrictions**: Only SystemAdmin users can edit domain configuration
4. **Read-Only Mode**: Non-SystemAdmin users see settings but cannot modify them
5. **Intuitive Form**: Clear labels and placeholders for each field
6. **Action Buttons**: Save and test functionality (only for SystemAdmin)
7. **Developer Mode**: Special section for testing (when activated)
8. **Visual Hierarchy**: Frames and colors to organize sections
9. **Responsive Layout**: Grids adapt to different screen sizes

## User Experience

- **Production Users**: See their current authorization info, cannot modify domain settings
- **DatabaseAdmin Users**: See domain settings but cannot change them (read-only)
- **SystemAdmin Users**: Full access to configure and test domain settings
- **Developers**: Access special testing features through hidden developer mode
- **Security**: Clear visual indicators when access is restricted
- **Error Handling**: Clear feedback through alerts and validation messages

This provides a complete, user-friendly interface for managing domain configuration without ever needing to edit code files!
