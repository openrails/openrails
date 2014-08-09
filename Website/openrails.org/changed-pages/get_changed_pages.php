<?php
require_once('../shared/mysql/db_connect.php');

// Retrieve id from cookie which should have been set at this point.
$cookie_name = 'or_org3';
if(!isset($_COOKIE[$cookie_name])) { die('Cookies required'); }

$id = $_COOKIE[$cookie_name];

$sql = 
  "SELECT UNIX_TIMESTAMP(updated_on) AS 'updated_on', MAX(UNIX_TIMESTAMP(visited_on)) AS 'visited_on', url, menu_path"
. " FROM tVisit, tWebpage AS t1"
. " WHERE made_to = url"
. " AND made_by = '$id'"
. " AND updated_on >" // -- Most recent visit to that page by that user
. "  (SELECT MAX(visited_on) FROM tVisit"
. "   WHERE made_to = t1.url"
. "   AND made_by = '$id'"
. "  )"
. " GROUP BY updated_on, url, menu_path"

. " UNION" // -- Combine with any webpages never visited

. " SELECT UNIX_TIMESTAMP(updated_on), NULL, url, menu_path"
. " FROM tWebpage"
. " WHERE url NOT IN (SELECT made_to FROM tVisit WHERE made_by = '$id')"
. " GROUP BY updated_on, url"

. " ORDER BY updated_on; -- Oldest change first";
#echo "$sql <br>";
$result = mysqli_query($dbc, $sql) or die("Error in query: '$sql'");
$row_count = mysqli_num_rows($result);
if ($row_count == 1) {
  echo("1 webpage");
}else{
  echo("$row_count webpages");
}
echo(" changed since your last visit.<br><br>");
echo("<table>");
echo("<tr><th>Updated on</th><th>Visited on</th><th>Webpage</th></tr>");
while($row = mysqli_fetch_assoc($result)) {
  echo("<tr><td class='date'>" . date("d-M-y", $row['updated_on']) . "</td><td class='date'>" . date("d-M-y", $row['visited_on'])  . "</td><td><a href='" .  $row['url'] . "'>" .  $row['menu_path'] . "</a></td></tr>");
}
echo("</table>");
?>