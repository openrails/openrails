<?php
require_once('/shared/mysql/db_connect.php');

// Retrieve id from cookie which should have been set at this point.
if(!isset($_COOKIE['or_org'])) { die('Cookies required'); }

$id = $_COOKIE['or_org'];

$sql = "SELECT MAX(UNIX_TIMESTAMP(updated_on)) AS 'updated_on', MAX(UNIX_TIMESTAMP(visited_on)) AS 'visited_on', url, menu_path"
. " FROM tVisit, tWebpage AS t1"
. " WHERE made_to = url"
. " AND made_by = '$id'"
. " AND updated_on >" // -- Most recent visit to that page by that user
. "  (SELECT MAX(visited_on) FROM tVisit"
. "   WHERE made_to = t1.url"
. "   AND made_by = '$id'"
. "  )"
. " GROUP BY url"
. " ORDER BY MAX(updated_on); -- Oldest change first";
//echo "$sql <br>";
$result = mysqli_query($dbc, $sql) or die("Error in query: '$sql'");
$row_count = mysqli_num_rows($result);
if ($row_count == 1) {
  echo("1 webpage changed.<br><br>");
}else{
  echo("$row_count webpages changed.<br><br>");
}
echo("<table>");
echo("<tr><th>Updated on</th><th>Visited on</th><th>Webpage</th></tr>");
while($row = mysqli_fetch_assoc($result)) {
  echo("<tr><td class='date'>" . date("d-M-y", $row['updated_on']) . "</td><td class='date'>" . date("d-M-y", $row['visited_on'])  . "</td><td><a href='http://" .  $row['url'] . "'>" .  $row['menu_path'] . "</a></td></tr>");
}
echo("</table>");
?>