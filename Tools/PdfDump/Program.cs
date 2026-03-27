using UglyToad.PdfPig;

var path = args.Length > 0 ? args[0] : @"D:\Bkp\GRID\GridAcademy\sample\2022_2_English.pdf";
using var doc = PdfDocument.Open(path);

foreach (var page in doc.GetPages())
{
    Console.WriteLine($"=== PAGE {page.Number} ===");
    Console.WriteLine(page.Text);
    Console.WriteLine();
}
