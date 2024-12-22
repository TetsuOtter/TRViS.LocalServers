using System;

#if NET48
using BveEx.Extensions.ContextMenuHacker;
#endif

namespace TRViS.LocalServers.BveTs;

public partial class TimetableServerPlugin
{
	void AddLaunchBrowserButtonToContextMenu()
	{
#if NET48
		Extensions.AllExtensionsLoaded += (_, _) =>
		{
			IContextMenuHacker ctxMenuHacker = Extensions.GetExtension<IContextMenuHacker>();

			ctxMenuHacker.AddClickableMenuItem("TRViS用QRコードを表示", OnOpenBrowserClicked, ContextMenuItemType.CoreAndExtensions);
		};
#endif
	}

	void OnOpenBrowserClicked(object? semder, EventArgs e) => server.OnOpenBrowserClicked();
}
