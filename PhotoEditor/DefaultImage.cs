using System.Drawing;

namespace PhotoEditor
{
	class DefaultImage
	{
		public int ID { get; private set; }
		public string Path { get; private set; }
		public string Name { get; private set; }
		public string Extension { get; private set; }
		public Size Size { get; private set; }
		public Image Image { get; set; }

		public DefaultImage(string p_path, int id)
		{
			ID = id;
			Path = p_path;
			Image = Image.FromFile(p_path);
			Size = Image.Size;
			Name = p_path.Substring(p_path.LastIndexOf('\\') + 1).Split('.')[0];
			Extension = p_path.Substring(p_path.LastIndexOf('.'));
		}

		~DefaultImage()
		{
			Image.Dispose();
		}
	}
}
