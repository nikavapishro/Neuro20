byte isFrameSynced = 0;
string bufferString = bufReceiedData.ToString();
while (bufferString.Length > Constants.BUFLENCHECK && nTotPacketRecieved< 200)
{
    byte idx = 0;
//byte[] chData = bufferString. ;
int nIndexCounter = 0;
    for(nIndexCounter = 0; nIndexCounter<bufferString.Length; nIndexCounter++)
    {
        nTotPacketRecieved++;
        int value = Convert.ToInt32(bufferString[nIndexCounter]);
        if (nTotPacketRecieved< 200) {
            Commdata.AppendText(value.ToString()+" ");        
        }
        //para.Inlines.Add(value.ToString());
        //mcFlowDoc.Blocks.Add(para);
        if (value == Constants.HEADER[idx])
        {
            //nTotPacketRecieved++;
            idx++;
            if (idx == Constants.HDRLEN)
            {
                isFrameSynced = 1;
                break;
            }
        }
        else
        {
            idx = 0;
        }
    }
    if (isFrameSynced == 0)
    {
        break;
    }
    else
    {
        nTotPacketRecieved++;
        isFrameSynced = 0;
        bufferString.Remove(0, nIndexCounter + Constants.FRMLEN);
    }
}
bufReceiedData = new StringBuilder(bufferString);




                    
if (nTotPacketRecieved == 1) {
                        for(int i=0; i<Constants.PACKETLEN + Constants.HDRLEN; i++)
                        {
                            int value = Convert.ToInt32(bufferString[index + i]);
Commdata.AppendText(value.ToString()+" ");        
                        }
                    }




    <Border Grid.Column="5" BorderBrush= "Gray" IsHitTestVisible= "False" BorderThickness= "1" CornerRadius= "4" Padding= "0" Margin= "0" >
                < Border.OpacityMask >
                    < VisualBrush >
                        < VisualBrush.Visual >
                            < Border Background= "Black" SnapsToDevicePixels= "True"
                            CornerRadius= "4"
                            Width= "{Binding ActualWidth, RelativeSource={RelativeSource FindAncestor, AncestorType=Border}}"
                            Height= "{Binding ActualHeight, RelativeSource={RelativeSource FindAncestor, AncestorType=Border}}" />
                        </ VisualBrush.Visual >
                    </ VisualBrush >
                </ Border.OpacityMask >
                < ComboBox x:Name= "cmbLowPassFilter" HorizontalAlignment= "Stretch" ItemsSource= "{Binding Source={StaticResource FilterLowPass}}" />
            </ Border >



< Border Grid.Column= "1" >
                < Border.OpacityMask >
                    < VisualBrush >
                        < VisualBrush.Visual >
                            < Border Background= "Black" SnapsToDevicePixels= "True"
                            CornerRadius= "4"
                            Width= "{Binding ActualWidth, RelativeSource={RelativeSource FindAncestor, AncestorType=Border}}"
                            Height= "{Binding ActualHeight, RelativeSource={RelativeSource FindAncestor, AncestorType=Border}}" />
                        </ VisualBrush.Visual >
                    </ VisualBrush >
                </ Border.OpacityMask >
                < Grid >
                    < Grid.ColumnDefinitions >
                        < ColumnDefinition Width= "*" />
                        < ColumnDefinition Width= "20" />
                    </ Grid.ColumnDefinitions >
                    < Grid.RowDefinitions >
                        < RowDefinition Height= "13" />
                        < RowDefinition Height= "13" />
                    </ Grid.RowDefinitions >
                    < TextBox x:Name= "txtTimeDiv" Grid.Column= "0" Grid.Row= "0" Grid.RowSpan= "2" x:FieldModifier= "private" Width= "50" VerticalAlignment= "Center" HorizontalAlignment= "Center" TextAlignment= "Center" Text= "0" IsReadOnly= "True" />
                    < Button x:Name= "cmdTimeDivUp" Grid.Column= "1" Grid.Row= "0" x:FieldModifier= "private" FontSize= "8" VerticalContentAlignment= "Center" HorizontalContentAlignment= "Center" Content= "˄" Click= "cmdTimeDivUp_Click" />
                    < Button x:Name= "cmdTimeDivDown" Grid.Column= "1" Grid.Row= "1" x:FieldModifier= "private" FontSize= "8" VerticalContentAlignment= "Center" HorizontalContentAlignment= "Center" Content= "˅"  Click= "cmdTimeDivDown_Click" />
                    < Border BorderBrush= "Gray" IsHitTestVisible= "False" BorderThickness= "1" CornerRadius= "4" Grid.RowSpan= "2" Grid.ColumnSpan= "2" Padding= "0" Margin= "0" />
                </ Grid >
            </ Border >
			
			
			<Border Grid.Column="3">
                <Border.OpacityMask>
                    <VisualBrush>
                        <VisualBrush.Visual>
                            <Border Background="Black" SnapsToDevicePixels="True"
                            CornerRadius="4"
                            Width="{Binding ActualWidth, RelativeSource={RelativeSource FindAncestor, AncestorType=Border}}"
                            Height="{Binding ActualHeight, RelativeSource={RelativeSource FindAncestor, AncestorType=Border}}" />
                        </VisualBrush.Visual>
                    </VisualBrush>
                </Border.OpacityMask>
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="20" />
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="13" />
                        <RowDefinition Height="13" />
                    </Grid.RowDefinitions>
                    <TextBox x:Name="txtVoltDiv" Grid.Column="0" Grid.Row="0" Grid.RowSpan="2" x:FieldModifier="private"  VerticalAlignment="Center" HorizontalAlignment="Stretch" TextAlignment="Center" Text="0" IsReadOnly="True" />
                    <Button x:Name="cmdVoltDivUp" Grid.Column="1" Grid.Row="0" x:FieldModifier="private" FontSize="8"  VerticalContentAlignment="Center" HorizontalContentAlignment="Center" Content="˄" />
                    <Button x:Name="cmdVoltDivDown" Grid.Column="1" Grid.Row="1" x:FieldModifier="private" FontSize="8" VerticalContentAlignment="Center" HorizontalContentAlignment="Center" Content="˅"  />
                    <Border BorderBrush="Gray" IsHitTestVisible="False" BorderThickness="1" CornerRadius="4" Grid.RowSpan="2" Grid.ColumnSpan="2" Padding="0" Margin="0" />
                </Grid>
            </Border>