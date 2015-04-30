using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace VSPackageManager
{
	public partial class Form1 : Form
	{
		private static readonly string PackagesPath = @"Software\Microsoft\VisualStudio\12.0_Config\Packages";
		List<ListViewItem> _items = new List<ListViewItem>();

		public Form1()
		{
			InitializeComponent();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			RefreshList();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			RefreshList();
		}

		bool _loading;
		private void RefreshList()
		{
			_loading = true;

			try
			{
				using (var packagesKey = Registry.CurrentUser.OpenSubKey(PackagesPath))
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
				_loading = false;
			}
		}

		private void ListViewItemChecked(object sender, ItemCheckedEventArgs e)
		{
			if (_loading)
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

			using (var packagesKey = Registry.CurrentUser.OpenSubKey(PackagesPath, true))
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
			_loading = true;
			try
			{
			listView1.Items.Clear();
			foreach (var item in _items)
			{
				if(
					!string.IsNullOrWhiteSpace(textBox1.Text)
					&&
					!item.Text.ToLower().Contains(textBox1.Text.ToLower())
					)
					continue;

				listView1.Items.Add(item);
			}
			}
			finally
			{
				_loading = false;
			}
		}
	}

	public static class RegistryUtilities
	{
		public static bool RenameSubKey(RegistryKey parentKey, string subKeyName, string newSubKeyName)
		{
			CopyKey(parentKey, subKeyName, newSubKeyName);
			parentKey.DeleteSubKeyTree(subKeyName);
			return true;
		}
		public static bool CopyKey(RegistryKey parentKey, string keyNameToCopy, string newKeyName)
		{
			using (RegistryKey destinationKey = parentKey.CreateSubKey(newKeyName))
			{
				using (RegistryKey sourceKey = parentKey.OpenSubKey(keyNameToCopy))
				{
					RecurseCopyKey(sourceKey, destinationKey);
				}
			}
			return true;
		}

		private static void RecurseCopyKey(RegistryKey sourceKey, RegistryKey destinationKey)
		{
			foreach (string valueName in sourceKey.GetValueNames())
			{
				object objValue = sourceKey.GetValue(valueName);
				RegistryValueKind valKind = sourceKey.GetValueKind(valueName);
				destinationKey.SetValue(valueName, objValue, valKind);
			}

			foreach (string sourceSubKeyName in sourceKey.GetSubKeyNames())
			{
				using (RegistryKey sourceSubKey = sourceKey.OpenSubKey(sourceSubKeyName))
				{
					using (RegistryKey destSubKey = destinationKey.CreateSubKey(sourceSubKeyName))
					{
						RecurseCopyKey(sourceSubKey, destSubKey);
					}
				}
			}
		}
	}

}
