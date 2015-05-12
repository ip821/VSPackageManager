using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace VSPackageManager
{
	public partial class Form1 : Form
	{
		private static readonly string VSPath = @"Software\Microsoft\VisualStudio\";
		private static readonly string PackagesPath = @"\Packages";
		List<ListViewItem> _items = new List<ListViewItem>();

		public Form1()
		{
			InitializeComponent();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			RefreshAll();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			RefreshAll();
		}

		bool _refreshingComboBox;
		private void RefreshAll()
		{
			_refreshingComboBox = true;

			try
			{
				comboBox1.Items.Clear();
				using (var vsKey = Registry.CurrentUser.OpenSubKey(VSPath))
				{
					var subkeyNames = vsKey.GetSubKeyNames();
					comboBox1.Items.AddRange(subkeyNames.Where(t => t.Contains("Config")).ToArray());
					comboBox1.SelectedIndex = 0;
				}

				RefreshList();
			}
			finally
			{
				_refreshingComboBox = false;
			}
		}

		bool _refreshingList;
		private void RefreshList()
		{
			_refreshingList = true;

			try
			{
				_items.Clear();
				using (var packagesKey = Registry.CurrentUser.OpenSubKey(VSPath + comboBox1.SelectedItem + PackagesPath))
				{
					var subkeyNames = packagesKey.GetSubKeyNames();
					foreach (var item in subkeyNames)
					{
						using (var packageKey = packagesKey.OpenSubKey(item))
						{
							var name = (string)packageKey.GetValue(null);
							var desc = (string)packageKey.GetValue("ProductName");
							var listViewItem = new ListViewItem
							{
								Text = name,
								Checked = !item.StartsWith("!"),
								Tag = item
							};
							listViewItem.SubItems.Add(new ListViewItem.ListViewSubItem { Text = desc });
							_items.Add(listViewItem);
						}
					}
				}
				Filter();
			}
			finally
			{
				_refreshingList = false;
			}
		}

		private void ListViewItemChecked(object sender, ItemCheckedEventArgs e)
		{
			if (_refreshingComboBox || _refreshingList)
				return;

			string keyName = (string)e.Item.Tag;
			string newKeyName = string.Empty;
			if (e.Item.Checked)
			{
				newKeyName = keyName.Replace("!", string.Empty);
			}
			else
			{
				newKeyName = "!" + keyName;
			}

			using (var packagesKey = Registry.CurrentUser.OpenSubKey(VSPath + comboBox1.SelectedItem + PackagesPath, true))
			{
				RegistryUtilities.RenameSubKey(packagesKey, keyName, newKeyName);
				e.Item.Tag = newKeyName;
			}
		}

		private void textBox1_TextChanged(object sender, EventArgs e)
		{
			Filter();
		}

		private void Filter()
		{
			_refreshingList = true;
			try
			{
			listView1.Items.Clear();
			foreach (var item in _items)
			{
				if(
					!string.IsNullOrWhiteSpace(textBox1.Text)
					&&
					(
						!item.Text.ToLower().Contains(textBox1.Text.ToLower())
						&&
						!item.SubItems[1].Text.ToLower().Contains(textBox1.Text.ToLower())
					)
					)
					continue;

				listView1.Items.Add(item);
			}
			}
			finally
			{
				_refreshingList = false;
			}
		}

		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			RefreshList();
		}

		private void _loadButton_Click(object sender, EventArgs e)
		{
			using (var dialog = new OpenFileDialog())
			{
				dialog.Filter = "Profile files|*.vspm";
				if (dialog.ShowDialog(this) != DialogResult.OK)
					return;

				var path = dialog.FileName;
				using (var streamReader = new StreamReader(path))
				{
					var data = streamReader.ReadToEnd();
					var disabledItems = JsonConvert.DeserializeObject<List<string>>(data);
					foreach (ListViewItem item in listView1.Items)
					{
						if (!disabledItems.Contains((string)item.Tag))
							continue;

						item.Checked = false;
					}
				}
			}
		}

		private void _saveButton_Click(object sender, EventArgs e)
		{
			using (var dialog = new SaveFileDialog())
			{
				dialog.Filter = "Profile files|*.vspm";
				if (dialog.ShowDialog(this) != DialogResult.OK)
					return;

				var path = dialog.FileName;

				var disabledItems = 
					listView1
					.Items
					.Cast<ListViewItem>()
					.Where(t => !t.Checked)
					.Select(t => t.Tag)
					.Cast<string>()
					.Select(t => t.Replace("!", string.Empty))
					.ToList();

				var data = JsonConvert.SerializeObject(disabledItems);
				using (var streamWriter = new StreamWriter(path))
				{
					streamWriter.Write(data);
				}
			}
		}
	}
}
