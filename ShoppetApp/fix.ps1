$file = "C:\Users\WINDOWS11\source\repos\Shoppet\ShoppetApp\Pages\PetPassportPage.xaml"
$content = Get-Content $file -Raw
$content = $content.Replace("âÂ‹", "‹")
$content = $content.Replace("âœ:", "☎")
$content = $content.Replace("âÓ", "✓")
$content = $content.Replace("ñŸU ", "🕐")
$content = Get-Content $file -Raw
$content = $content.Replace("âÓ", "✓")
$content = $content.Replace("ñŸU ", "🕐")

