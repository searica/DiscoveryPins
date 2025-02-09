<div class="header">
	<h2>Versions 0.X.X</h2>
</div>
<table>
	<tbody>
		<tr>
			<th align="center">Version</th>
			<th align="center">Notes</th>
		</tr>
		<tr>
			<td align="center">0.3.7</td>
			<td align="left">
				<ul>
					<li>Added flametal pinning.</li>
					<li>Fixed some infrequent bug where ores that spawn as part of a location not being pinnable.</li>
				</ul>
			</td>
		</tr>
		<tr>
			<td align="center">0.3.6</td>
			<td align="left">
				<ul>
					<li>Fix deleted pins reappearing.</li>
				</ul>
			</td>
		</tr>
		<tr>
			<td align="center">0.3.4/0.3.5</td>
			<td align="left">
				<ul>
					<li>Fixed bug where pins were only colored when you zoomed out enough.</li>
				</ul>
			</td>
		</tr>
		<tr>
			<td align="center">0.3.3</td>
			<td align="left">
				<ul>
					<li>Fixed bug that meant pins were not recolored when placed unless you named them.</li>
				</ul>
			</td>
		</tr>
		<tr>
			<td align="center">0.3.2</td>
			<td align="left">
				<ul>
					<li>Fixed bug that prevented portals being pinned when you placed them.</li>
					<li>Increased default and max values for Pin Spacing.</li>
					<li>Added auto-pin options for Obsidian, Giant remains, and Tar (all considered Ore).</li>.
					<li>Improved automatic localization of ore pin names.</li>
					<li>Fixed bug that prevent auto-pinning of overworld dungeons like camps and villages.</li>
					<li>Improved detection of objects on screen when using the auto-pin shortcut. Now dymanically samples more points in the location the larger the object is.</li>
				</ul>
			</td>
		</tr>
		<tr>
			<td align="center">0.3.1</td>
			<td align="left">
				<ul>
					<li>Fixed exception caused by a typo.</li>
				</ul>
			</td>
		</tr>
		<tr>
			<td align="center">0.3.0</td>
			<td align="left">
				<ul>
					<li>Added pin spacing config.</li>
					<li>Fixed possible bug where deleted pins could reappear under specific circumstances.</li>
				</ul>
			</td>
		</tr>
		<tr>
			<td align="center">0.2.4</td>
			<td align="left">
				<ul>
					<li>Only pin portals when you first build them rather than whenever you load in near ones you built.</li>
					<li>More adjustments to auto-pinning large objects, now checks face diagonal points of bounding box.</li>
					<li>Auto-remove pins when the piece that triggered the auto-pin on placement is removed.</li>
				</ul>
			</td>
		</tr>
		<tr>
			<td align="center">0.2.3</td>
			<td align="left">
				<ul>
					<li>Detect whether points that are being checked are underground and prevent auto-pinning if they are.</li>
				</ul>
			</td>
		</tr>
		<tr>
			<td align="center">0.2.2</td>
			<td align="left">
				<ul>
					<li>Improved detection of if you are looking at things like large ore deposits.</li>
					<li>Reduced default range of auto-pin shortcut and reduced maximum range. This was needed due to improved detection.</li>
					<li>Reduced the vertical distance allowance for checking if something is "close enough" to avoid trivailizing find things underground.</li>
				</ul>
			</td>
		</tr>
		<tr>
			<td align="center">0.2.1</td>
			<td align="left">
				<ul>
					<li>Fixed issue regarding portal pins being named incorrectly.</li>
				</ul>
			</td>
		</tr>
		<tr>
			<td align="center">0.2.0</td>
			<td align="left">
				<ul>
					<li>Fixed sunken crypts not auto pinning.</li>
					<li>Added auto pinning of portals.</li>
					<li>Tweaked auto pin shortcut logic to make pinning things that have the center of them underground.</li>
					<li>Tweaked auto pin shortcut logic to allow pinning things if you standing on top of them even if you are not looking at them.</li>
					<li>Changed default auto-pin keybind to `LeftShift` + `RightClick`.</li>
					<li>Fixed duplicate numbering on auto-pins sections in config file.</li>
				</ul>
			</td>
		</tr>
		<tr>
			<td align="center">0.1.2</td>
			<td align="left">
				<ul>
					<li>Changed default player pin color to white.</li>
				</ul>
			</td>
		</tr>
		<tr>
			<td align="center">0.1.1</td>
			<td align="left">
				<ul>
					<li>Fixed manifest description.</li>
				</ul>
			</td>
		</tr>
		<tr>
			<td align="center">0.1.0</td>
			<td align="left">
				<ul>
					<li>Initial release.</li>
				</ul>
			</td>
		</tr>
	</tbody>
</table>