using System.Drawing;
using System.Drawing.Imaging;
using TouchpadDraw;

var output=Path.GetFullPath(args.Length>0?args[0]:"src/TouchpadDraw.App/Assets");
Directory.CreateDirectory(output);
var variants=new Dictionary<string,Color?> { ["app"]=null, ["paused"]=Color.FromArgb(133,150,145), ["ready"]=Color.FromArgb(177,222,91), ["drawing"]=Color.FromArgb(38,196,157), ["error"]=Color.FromArgb(234,105,85) };
foreach(var (name,color) in variants)
{
    var sizes=new[]{16,20,24,32,40,48,64,128,256};
    var frames=new List<byte[]>();
    foreach(var size in sizes)
    {
        using var bitmap=BrandAssets.RenderLogo(size,color); using var png=new MemoryStream();
        bitmap.Save(png,ImageFormat.Png); frames.Add(png.ToArray());
    }
    using var file=File.Create(Path.Combine(output,name+".ico")); using var writer=new BinaryWriter(file);
    writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)sizes.Length);
    var offset=6+16*sizes.Length;
    for(var i=0;i<sizes.Length;i++)
    {
        writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)(sizes[i]==256?0:sizes[i]));
        writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)1);writer.Write((ushort)32);
        writer.Write(frames[i].Length);writer.Write(offset);offset+=frames[i].Length;
    }
    foreach(var frame in frames)writer.Write(frame);
    Console.WriteLine(Path.Combine(output,name+".ico"));
}
using(var logo=BrandAssets.RenderLogo(512))logo.Save(Path.Combine(output,"logo.png"),ImageFormat.Png);
using(var preview=new Bitmap(560,160))
{
    using var g=Graphics.FromImage(preview);g.Clear(Color.FromArgb(242,245,244));
    using var font=new Font("Segoe UI",9);
    var x=20;
    foreach(var size in new[]{16,24,32,48,64,96})
    {
        using var logo=BrandAssets.RenderLogo(size);g.DrawImage(logo,x,30,size,size);g.DrawString(size+"px",font,Brushes.DimGray,x,135);x+=size+24;
    }
    preview.Save(Path.Combine(output,"logo-sizes.png"),ImageFormat.Png);
}
