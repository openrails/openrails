unit Unit1;

{$mode objfpc}{$H+}

interface

uses
  Classes, SysUtils, FileUtil, Forms, Controls, Graphics, Dialogs, ExtCtrls,
  Grids, Buttons, Menus, Spin, LazUTF8, charencstreams, timetabledata, about, clipbrd,
  defaulttranslator, StdCtrls, Contnrs;

type

  { TForm1 }

  TForm1 = class(TForm)
    MenuItem1: TMenuItem;
    MenuItem2: TMenuItem;
    MenuItem3: TMenuItem;
    MenuItem4: TMenuItem;
    MenuItem5: TMenuItem;
    MenuItem6: TMenuItem;
    MenuItem7: TMenuItem;
    MenuItem8: TMenuItem;
    OpenDialog1: TOpenDialog;
    Panel1: TPanel;
    grid: TStringGrid;
    gridpopup: TPopupMenu;
    Panel2: TPanel;
    SaveDialog1: TSaveDialog;
    SaveDialog2: TSaveDialog;
    SpeedButton1: TSpeedButton;
    Infobutton: TSpeedButton;
    SpeedButton10: TSpeedButton;
    SpeedButton11: TSpeedButton;
    SpeedButton12: TSpeedButton;
    SpeedButton13: TSpeedButton;
    SpeedButton14: TSpeedButton;
    SpeedButton15: TSpeedButton;
    SpeedButton16: TSpeedButton;
    SpeedButton2: TSpeedButton;
    SpeedButton3: TSpeedButton;
    SpeedButton4: TSpeedButton;
    SpeedButton5: TSpeedButton;
    SpeedButton6: TSpeedButton;
    SpeedButton7: TSpeedButton;
    SpeedButton8: TSpeedButton;
    SpeedButton9: TSpeedButton;
    SpinEdit1: TSpinEdit;
    SpinEdit2: TSpinEdit;
    procedure FormCloseQuery(Sender: TObject; var CanClose: boolean);
    procedure FormCreate(Sender: TObject);
    procedure gridEditingDone(Sender: TObject);
    procedure gridEnter(Sender: TObject);
    procedure gridExit(Sender: TObject);
    procedure gridGetEditText(Sender: TObject; ACol, ARow: Integer;
      var Value: string);
    procedure gridKeyPress(Sender: TObject; var Key: char);
    procedure gridSelectCell(Sender: TObject; aCol, aRow: Integer;
      var CanSelect: Boolean);
    procedure InfobuttonClick(Sender: TObject);
    procedure MenuItem1Click(Sender: TObject);
    procedure MenuItem3Click(Sender: TObject);
    procedure MenuItem4Click(Sender: TObject);
    procedure MenuItem5Click(Sender: TObject);
    procedure MenuItem7Click(Sender: TObject);
    procedure MenuItem8Click(Sender: TObject);
    procedure SpeedButton10Click(Sender: TObject);
    procedure SpeedButton11Click(Sender: TObject);
    procedure SpeedButton12Click(Sender: TObject);
    procedure SpeedButton13Click(Sender: TObject);
    procedure SpeedButton14Click(Sender: TObject);
    procedure SpeedButton15Click(Sender: TObject);
    procedure SpeedButton16Click(Sender: TObject);
    procedure SpeedButton1Click(Sender: TObject);
    procedure SpeedButton2Click(Sender: TObject);
    procedure SpeedButton3Click(Sender: TObject);
    procedure resetgrid();
    procedure autosizecol(Gridt: TStringGrid; Column: integer);
    function getlastCell():string;
    procedure SpeedButton4Click(Sender: TObject);
    procedure SpeedButton5Click(Sender: TObject);
    function getTitle(): string;
    procedure setTitle(title: String);
    procedure SpeedButton6Click(Sender: TObject);
    procedure SpeedButton7Click(Sender: TObject);
    procedure SpeedButton8Click(Sender: TObject);
    procedure SpeedButton9Click(Sender: TObject);
    procedure SpinEdit1Change(Sender: TObject);
    procedure SpinEdit2Change(Sender: TObject);
    procedure setFixed();
    procedure enableButtons();
    function gettrains(static: boolean): tstringlist;
    procedure shifttimes(zeit: string; spalte: integer);
    procedure shadowupdate;
    procedure restoreshadow(nr: integer);
  private
    { private declarations }
    fCES: TCHarEncStream;
  public
    { public declarations }
  end;

var
  Form1: TForm1;
  copylist: tstringlist;
  TTfilename: String;
  shadowlist: tobjectlist;
  shadow: integer;
  allowupdate: boolean;
  unsaved: boolean;

type
  timearray = array[0..2] of integer;

resourceString
  irgendwas = 'irgendas';
  saveTimeTable = 'The timetable has to be saved in the folder "Activities\Openrails\" of the route. Should the folder be created?';
  InpTTTitle   = 'Timetable title';
  QuestTTTitle = 'The timetable needs a title, so Openrails can read it. Please choose a title';
  DLGsave = 'save timetable';
  DLGopenRDB = 'open route';
  DLGopenTT = 'open timetable';
  copynotempty = 'The column is not empty, overwrite the current text?';
  delrownotempty = 'The row contains data, delete it?';
  delcolnotempty = 'The column contains data, delete it?';
  timeadd = 'How much time do you want to add?';
  filealreadyexists = 'The file already exists. Save it with this name?';
  saveChanges = 'Do you want to save your changes before closing?';
  savezip = 'save zip';
  DLGImportTT = 'Import Timetable';

implementation

uses stations, trains, dispose, sidings;

{$R *.lfm}

{ TForm1 }

procedure tform1.shadowupdate;
var sgrid: tstringgrid;
    r,c: integer;
begin
  if shadowlist.count = 13 then shadowlist.delete(0);
  if shadow < shadowlist.count -1 then begin
   for r:=shadowlist.count -1 downto shadow do begin
     shadowlist.Delete(r);
   end;
  end;
//  memo1.lines.add('shadows:'+inttostr(shadowlist.count)+' shadow:'+inttostr(shadow));
  sgrid:=tstringgrid.create(self);
  sgrid.rowcount:=grid.rowcount;
  sgrid.colcount:=grid.colcount;
  sgrid.fixedrows:=grid.FixedRows;
  sgrid.fixedcols:=grid.fixedcols;
  for r:=0 to grid.rowcount -1 do begin
    for c:=0 to grid.colcount -1 do begin
      sgrid.cells[c,r]:=grid.cells[c,r];
    end;
  end;
  shadowlist.add(sgrid);
  shadow:=shadowlist.count-1;
  speedbutton13.enabled:=false;
  unsaved:=true;
end;

procedure tform1.restoreshadow(nr: integer);
var shadowgrid: tstringgrid;
    c,r: integer;
begin
  allowupdate:=false;
  shadowgrid:=(shadowlist.items[nr-1]) as TStringgrid;
  grid.colcount:=shadowgrid.colcount;
  grid.rowcount:=shadowgrid.rowcount;
  grid.FixedRows:=shadowgrid.fixedrows;
  grid.FixedCols:=shadowgrid.fixedcols;
  for r:=0 to shadowgrid.RowCount -1 do begin
    for c:=0 to shadowgrid.colcount -1 do begin
      grid.cells[c,r]:=shadowgrid.cells[c,r];
    end;
  end;
//  memo1.lines.add('shadow:'+inttostr(nr-1)+' shadowlistcount:'+inttostr(shadowlist.count));
  //allowupdate:=true;
end;

procedure tform1.resetgrid();
begin
  grid.FixedRows:=1;
  grid.fixedcols:=1;
  grid.clear;
  grid.ColCount:=30;
  grid.rowcount:=30;
end;

procedure tform1.setFixed();
var i: integer;
begin
  for i:=0 to grid.colcount -1 do begin
    if grid.Cells[i,0]='#comment' then begin
      spinedit2.Value:=i+1;
      break;
    end;
  end;
  for i:=0 to grid.rowcount -1 do begin
    if grid.cells[0,i]='#start' then begin
      spinedit1.value:=i+1;
      break;
    end;
  end;
end;

procedure tform1.enableButtons();
begin
  speedbutton6.enabled:=true;
  spinedit1.enabled:=true;
  spinedit2.enabled:=true;
  speedbutton5.enabled:=true;
  speedbutton4.enabled:=true;
  speedbutton3.enabled:=true;
  //speedbutton7.enabled:=true;
  speedbutton8.enabled:=true;
  speedbutton12.enabled:=true;
  speedbutton15.enabled:=true;
  speedbutton16.enabled:=true;
end;

procedure tform1.autosizecol(Gridt: TStringgrid; Column: integer);
var i,w,wmax: integer;
begin
  wmax:=0;
  for i:=0 to(gridt.rowcount -1) do begin
    w:= gridt.canvas.textwidth(gridt.cells[column,i]);
    if w = 0 then w:=gridt.defaultcolwidth;
    if w > wmax then wmax:=w;
  end;
  gridt.colwidths[column]:=wmax+5;
end;

function tform1.getlastCell():string;
var c,r,mc,mr: integer;
begin
  mc:=-1;
  mr:=-1;
  for c:= 0 to grid.ColCount -1 do begin
    for r:= 0 to grid.rowcount -1 do begin
      if grid.Cells[c,r]<>'' then begin
        if r > mr then mr:=r;
        if c > mc then mc:=c;
      end;
    end;
  end;
  result:=inttostr(mr)+':'+inttostr(mc);
end;

function tform1.getTitle(): string;
var c,cf,r,rf: integer;
begin
  cf:=-1;
  rf:=-1;
  for c:=0 to grid.colcount -1 do begin
    if grid.cells[c,0]='#comment' then begin
      cf:=c;
      break;
    end;
  end;
  for r:=0 to grid.rowcount -1 do begin
    if grid.cells[0,r]='#comment' then begin
      rf:=r;
      break;
    end;
  end;
  result:=grid.Cells[cf,rf];
end;

procedure tform1.setTitle(title: String);
var c,cf,r,rf: integer;
begin
  cf:=-1;
  rf:=-1;
  for c:=0 to grid.colcount -1 do begin
    if grid.cells[c,0]='#comment' then begin
      cf:=c;
      break;
    end;
  end;
  for r:=0 to grid.rowcount -1 do begin
    if grid.cells[0,r]='#comment' then begin
      rf:=r;
      break;
    end;
  end;
  grid.Cells[cf,rf]:=title;
end;

function Tform1.gettrains(static: boolean): tstringlist;
var i: integer;
    trains: tstringlist;
begin
  trains:=tstringlist.create;
  for i:=0 to grid.colcount -1 do begin
    if (grid.cells[i,0]<> '') and (grid.cells[i,0] <> '#comment') then begin
      if (( grid.cells[i,0]<> '$static' ) or ( static = true )) and (i<> grid.Col) then trains.add(grid.cells[i,0]);
    end;
  end;
  result:=trains;
end;

procedure TForm1.SpeedButton6Click(Sender: TObject);
begin
  if form1.ActiveControl = grid then form3.showmodal;
end;

procedure TForm1.SpeedButton7Click(Sender: TObject);
begin
  if grid.cells[0,grid.row] = '#dispose' then form6.showModal;
end;

procedure TForm1.SpeedButton8Click(Sender: TObject);
var r: integer;
begin
  copylist.Clear;
  for r:=0 to grid.RowCount -1 do begin
    copylist.add(grid.Cells[grid.Col,r]);
  end;
  speedbutton9.enabled:=true;
  speedbutton10.enabled:=true;
end;

procedure TForm1.SpeedButton9Click(Sender: TObject);
var col,r, but: integer;
    empty: boolean;
begin
  col:=grid.col;
  empty:=true;
  for r:=0 to grid.RowCount -1 do begin
    if grid.cells[col,r] <> '' then empty:=false;
  end;
  but:=0;
  if not empty then but:=messagedlg(copynotempty,mtConfirmation,[mbyes,mbno],0);
  if ( empty ) or (but=mryes) then begin
    if grid.rowcount < copylist.Count then begin
      for r:=grid.rowcount to copylist.count do begin
        grid.RowCount:=grid.rowcount +1;
      end;
    end;
    for r:=0 to copylist.count -1 do begin
      grid.cells[col,r]:=copylist[r];
    end;
  end;
  shadowupdate;
end;


procedure tform1.shifttimes(zeit: string; spalte: integer);
var r,i: integer;
    zt, erg: String;
    zshift, zstop, rzeit: timearray;
    times: tstringlist;
begin
  zshift:=splitzeit(zeit);
  times:=tstringlist.create;
  for r:=1 to grid.rowcount -1 do begin
    if pos('#start',trim(grid.cells[0,r])) > 0 then begin // Startzeiten bearbeiten
      zt:=grid.cells[spalte,r];
      if zt <> '' then begin
        times:=extracttime(zt);
        zstop:=splitzeit(times[0]);
        rzeit:=calctime(zstop, zshift);
        if rzeit[2] = -1 then erg:=returntime(rzeit[0])+':'+returntime(rzeit[1])
        else erg:=returntime(rzeit[0])+':'+returntime(rzeit[1])+':'+returntime(rzeit[2]);
        if times[1] <> '' then begin
          zstop:=splitzeit(times[1]);
          rzeit:=calctime(zstop,zshift);
          if rzeit[2] = -1 then erg:=erg+' $create='+returntime(rzeit[0])+':'+returntime(rzeit[1])
          else erg:=erg+' $create='+returntime(rzeit[0])+':'+returntime(rzeit[1])+':'+returntime(rzeit[2]);
        end;
        if times[2] <> '' then erg:=erg+' '+times[2];
        grid.cells[spalte,r]:=erg;
      end;
    end;
    if pos('#', trim(grid.cells[0,r])) <> 1 then begin // Haltezeiten bearbeiten
      zt:=grid.cells[spalte,r];
      if zt <> '' then begin
        times:=extracttime(zt);
        zstop:=splitzeit(times[0]);
        rzeit:=calctime(zstop, zshift);
        if rzeit[2] = -1 then erg:=returntime(rzeit[0])+':'+returntime(rzeit[1])
        else erg:=returntime(rzeit[0])+':'+returntime(rzeit[1])+':'+returntime(rzeit[2]);
        if times[1] <> '' then begin
          zstop:=splitzeit(times[1]);
          rzeit:=calctime(zstop,zshift);
          if rzeit[2] = -1 then erg:=erg+'-'+returntime(rzeit[0])+':'+returntime(rzeit[1])
          else erg:=erg+'-'+returntime(rzeit[0])+':'+returntime(rzeit[1])+':'+returntime(rzeit[2]);
        end;
        if times[2] <> '' then begin
          erg:=erg+' '+times[2];
        end;
        grid.cells[spalte,r]:=erg;
      end;
    end;
    if ( pos('#dispose', trim(grid.cells[0,r])) > 0 ) and ( grid.cells[spalte,r] <> '' ) then begin
      zt:=grid.cells[spalte,r];
      split('/',zt,times);
      for i:= 0 to times.count -1 do begin
        if pos('out_path',times[i]) >0 then times[i]:='/'+times[i];
        if pos('out_time',times[i]) > 0 then begin
          zstop:=splitzeit(trim(copy(times[i],pos('=',times[i])+1,length(times[i]))));
          rzeit:=calctime(zstop,zshift);
          if rzeit[2] = -1 then times[i]:='/out_time='+returntime(rzeit[0])+':'+returntime(rzeit[1])
          else times[i]:='/out_time='+returntime(rzeit[0])+':'+returntime(rzeit[1])+':'+returntime(rzeit[2]);
        end;
        if pos('in_path',times[i]) > 0 then times[i]:='/'+times[i];
        if pos('in_time',times[i]) > 0 then begin
          zstop:=splitzeit(trim(copy(times[i],pos('=',times[i])+1,length(times[i]))));
          rzeit:=calctime(zstop,zshift);
          if rzeit[2] = -1 then times[i]:='/in_time='+returntime(rzeit[0])+':'+returntime(rzeit[1])
          else times[i]:='/in_time='+returntime(rzeit[0])+':'+returntime(rzeit[1])+':'+returntime(rzeit[2]);
        end;
        if pos('runround',times[i]) > 0 then times[i]:='/'+times[i];
        if pos('rrtime',times[i]) > 0 then begin
          zstop:=splitzeit(trim(copy(times[i],pos('=',times[i])+1,length(times[i]))));
          rzeit:=calctime(zstop,zshift);
          if rzeit[2] = -1 then times[i]:='/rrtime='+returntime(rzeit[0])+':'+returntime(rzeit[1])
          else times[i]:='/rrtime='+returntime(rzeit[0])+':'+returntime(rzeit[1])+':'+returntime(rzeit[2]);
        end;
        if pos('rrpos',times[i]) > 0 then times[i]:='/'+times[i];
        if pos('set stop',times[i]) > 0 then times[i]:='/'+times[i];
        if pos('forms',times[i]) > 0 then times[i]:='/'+times[i];
        if pos('triggers',times[i]) > 0 then times[i]:='/'+times[i];
        if pos('static',times[i]) > 0 then times[i]:='/'+times[i];
      end;
      zt:='';
      for i:=0 to times.count -1 do begin
        zt:=zt+times[i]+' ';
      end;
      zt:=trim(zt);
      grid.cells[spalte,r]:=zt;
    end;
  end;
end;


procedure TForm1.SpinEdit1Change(Sender: TObject);
begin
  grid.FixedRows:=spinedit1.Value;
end;

procedure TForm1.SpinEdit2Change(Sender: TObject);
begin
  grid.fixedcols:=spinedit2.value;
end;


procedure TForm1.SpeedButton4Click(Sender: TObject);
var currentrow, r,c: integer;
    last: boolean;
begin
  currentrow:=grid.Row;
  last:=false;
  if currentrow=grid.RowCount -1 then last:=true;
  grid.rowCount:=grid.rowcount+1;
  if last = false then begin
    for r:= grid.rowcount -2 downto currentrow do begin
      for c:=0 to grid.ColCount -1 do begin
         grid.Cells[c,r+1]:=grid.cells[c,r];
      end;
    end;
    for c:=0 to grid.colcount -1 do begin
      grid.cells[c,currentrow]:='';
    end;
  end;
  shadowupdate;
end;

procedure TForm1.SpeedButton5Click(Sender: TObject);
var currentcol, r, c: integer;
    last: boolean;
begin
  currentcol:= grid.col;
  last:=false;
  if currentcol= grid.colcount -1 then last:=true;
  grid.colcount:=grid.colcount +1;
  if last = false then begin
    for c:= grid.ColCount -2 downto currentcol do begin
      for r:=0 to grid.rowcount -1 do begin
        grid.cells[c+1,r]:=grid.cells[c,r];
      end;
    end;
    for r:=0 to grid.rowcount -1 do begin
      grid.cells[currentcol,r]:='';
    end;
  end;
  shadowupdate;
end;

procedure TForm1.SpeedButton3Click(Sender: TObject);  // speichern
var lines: tstringlist;
    cell, line, title: String;
    cols, rows,c,r: integer;
    but: integer;
    save: boolean;
begin
    if not directoryexists(UTF8ToSys(getroutepath+'Activities\Openrails')) then begin
      if messagedlg(savetimetable, mtConfirmation, [mbyes,mbno],0) = mryes then begin
      //if messagedlg('Der Zeitplan muss im Ordner "Activities\Openrails\" der Strecke gespeichert werden. Soll der Ordner erstellt werden?', mtConfirmation, [mbyes,mbno],0) = mryes then begin
        createdir(UTF8ToSys(getroutepath+'Activities\Openrails'));
      end;
    end;
    if gettitle = '' then begin
      title:='';
       inputquery(InpTTTitle,QuestTTTitle,title);
        //inputquery('Zeitplan Titel','Der Zeitplan benötigt eien Titel, damit Openrails ihn verarbeiten kann. Bitte Titel eingeben',title);
        settitle(title);
    end;
    //savedialog1.Title:='Zeitplan speichern';
    savedialog1.Title:=DLGsave;
    savedialog1.InitialDir:=UTF8ToSys(getroutepath+'Activities\Openrails');
    //savedialog1.Filter:='Open Rails Zeitplan|*.timetable-or';
    savedialog1.Filter:='Open Rails timetable|*.timetable-or;*.timetable_or';
    if ttfilename <> '' then savedialog1.filename:=ttfilename
    else savedialog1.filename:=title+'.timetable-or';
    save:=true;
    but:=1;
    if (savedialog1.Execute) and (savedialog1.FileName<>'') then begin
      if fileexists(savedialog1.filename) then but:=messagedlg(filealreadyexists,mtConfirmation,[mbyes,mbno],0);
      if but = mrno then save:=false;
      if save then begin
        ttfilename:=savedialog1.filename;
        speedbutton11.enabled:=true;
        speedbutton14.enabled:=true;
        lines:=tstringlist.create;
        cell:=getlastCell();
        rows:=strtoint(copy(cell,1,pos(':',cell)-1));
        cols:=strtoint(copy(cell,pos(':',cell)+1,length(cell)));
        for r:=0 to rows do begin
          line:='';
          for c:=0 to cols do begin
            line:=line+grid.cells[c,r]+';';
          end;
          line:=copy(line,1,length(line)-1);
          lines.add(line);
        end;
        fCES:=TCharEncStream.create;
        fCES.reset;
        fces.HasBOM:=true;
        fces.HaveType:=true;
        fces.UniStreamType:=ufUtf16le;
        fces.UTF8Text:=lines.text;
        fces.SaveToFile(UTF8ToSys(savedialog1.filename));
        fces.Free;
        unsaved:=false;
      end;
    end;
end;

procedure TForm1.SpeedButton1Click(Sender: TObject);      // Neu
var tmp: tstringlist;
begin
  tmp:=tstringlist.create;
  opendialog1.Filter:='routedatabase (*.tdb)|*.tdb';
  opendialog1.filename:='';
  opendialog1.Title:=DLGopenRDB;
  ttfilename:='';
  if (opendialog1.Execute) and (opendialog1.filename <> '') then begin
    shadowlist.clear;
    grid.enabled:=true;
    resetgrid;
    grid.Cells[0,1]:='#comment';
    grid.cells[0,2]:='#path';
    grid.cells[0,3]:='#consist';
    grid.cells[0,4]:='#start';
    grid.cells[0,5]:='#comment';
    grid.cells[2,0]:='#comment';
    FCES:=TCHarEncStream.Create;
    FCES.reset;
    fCES.LoadFromFile(UTF8ToSys(opendialog1.filename));
    tmp.text:=fces.utf8text;
    //setroutepath(extractfilepath(opendialog1.filename));
    opentimetable(opendialog1.filename);
    extractstations(tmp);
    liststationsfiles;
    fces.free;
    enableButtons;
    form5.showmodal;
    shadowupdate;
    speedbutton11.enabled:=false;
    speedbutton14.enabled:=false;
  end;
end;

procedure TForm1.SpeedButton2Click(Sender: TObject);  // öffnen
var slist: tstringlist;
    z,s,mcols: integer;
    cols: tstringlist;
begin
  slist:=tstringlist.create;
  cols:=tstringlist.create;
  //opendialog1.Title:='Zeitplan öffnen';
  opendialog1.Title:=DLGopenTT;
  //opendialog1.Filter:='Open Rails Zeitplan|*.timetable-or';
  opendialog1.Filter:='Open Rails timetable|*.timetable-or;*.timetable_or';
  opendialog1.filename:='';
  if (opendialog1.execute) and (opendialog1.FileName<>'') then begin
    resetgrid;
    shadowlist.clear;
    ttfilename:=opendialog1.filename;
    opentimetable(opendialog1.filename);
    fces:=tcharencStream.create;
    fces.reset;
    fces.loadfromfile(UTF8ToSys(opendialog1.filename));
    slist.text:=fces.utf8text;
    split(';',slist[0],cols);
    mcols:=0;
    mcols:=cols.count;
    if mcols +1 > grid.colcount then grid.colcount:=mcols+1;
    if slist.count +1 > grid.rowcount then grid.rowcount:=slist.count+1;
    for z:=0 to slist.count -1 do begin
      split(';',slist[z],cols);
      for s:= 0 to cols.count -1 do begin
        grid.cells[s,z]:=cols[s];
      end;
    end;
    fces.free;
    grid.enabled:=true;
    for s:=0 to grid.colcount -1 do begin
      autosizecol(grid, s);
    end;
    enableButtons;
    grid.SetFocus;
    speedbutton11.enabled:=true;
    speedbutton14.enabled:=true;
    shadowupdate;
  end;
end;

procedure TForm1.FormCreate(Sender: TObject);
begin
  grid.align:=alclient;
  grid.enabled:=false;
  resetgrid;
  speedbutton6.enabled:=false;
  spinedit1.enabled:=false;
  spinedit2.enabled:=false;
  speedbutton5.enabled:=false;
  speedbutton4.enabled:=false;
  speedbutton3.enabled:=false;
  speedbutton7.enabled:=false;
  copylist:=tstringlist.create;
  ttfilename:='';
  shadowlist:=tobjectlist.create(false);
  shadow:=0;
  allowupdate:=true;
end;

procedure TForm1.FormCloseQuery(Sender: TObject; var CanClose: boolean);
begin
  if unsaved then begin
    if messagedlg(saveChanges,mtConfirmation,[mbYes,mbNo],0) = mrYes then begin
      canclose:=false;
      speedbutton3.click;
    end;
  end;
end;

procedure TForm1.gridEditingDone(Sender: TObject);
begin
  if allowupdate then shadowupdate;
end;

procedure TForm1.gridEnter(Sender: TObject);
begin
  allowupdate:=true;
end;

procedure TForm1.gridExit(Sender: TObject);
begin
  allowupdate:=false;
end;

procedure TForm1.gridGetEditText(Sender: TObject; ACol, ARow: Integer;
  var Value: string);
begin
  allowupdate:=true;
end;

procedure TForm1.gridKeyPress(Sender: TObject; var Key: char);
begin
  if key = #13 then begin

  end;
end;

procedure TForm1.gridSelectCell(Sender: TObject; aCol, aRow: Integer;
  var CanSelect: Boolean);
begin
  if grid.cells[0,arow]='#dispose' then speedbutton7.enabled:=true else speedbutton7.enabled:=false;
end;

procedure TForm1.InfobuttonClick(Sender: TObject);
begin
  form2.showmodal;
end;

procedure TForm1.MenuItem1Click(Sender: TObject);  // Spaltenbreite anpassen
begin
  autosizecol(grid, grid.Col);
end;

procedure TForm1.MenuItem3Click(Sender: TObject);  //Ausschneiden
begin
   Clipboard.AsText := grid.Cells[grid.col,grid.row];
   grid.cells[grid.col,grid.row]:='';
end;

procedure TForm1.MenuItem4Click(Sender: TObject);
begin
  Clipboard.AsText := grid.Cells[grid.col,grid.row];
end;

procedure TForm1.MenuItem5Click(Sender: TObject);
begin
  If Clipboard.HasFormat(CF_TEXT) then grid.cells[grid.col,grid.row]:=Clipboard.AsText;
end;

procedure TForm1.MenuItem7Click(Sender: TObject); // Zeile löschen
var currentrow,c,r,but: integer;
    empty: boolean;
begin
  empty:=true;
  currentrow:=grid.Row;
  for c:=0 to grid.colcount -1 do begin
    if grid.cells[c,currentrow] <> '' then empty:=false;
  end;
  if not empty then but:=messagedlg(delrownotempty,mtConfirmation,[mbyes,mbno],0);
  if ( empty ) or (but=mryes) then begin
    if currentrow < grid.RowCount -1 then begin
      for r:= currentrow to grid.RowCount -2 do begin
        for c:=0 to grid.ColCount -1 do begin
          grid.cells[c,r]:=grid.cells[c,r+1];
        end;
      end;
    end;
    grid.RowCount:=grid.rowcount -1;
  end;
  shadowupdate;
end;

procedure TForm1.MenuItem8Click(Sender: TObject); // Spalte löschen
var currentcol, c,r,but: integer;
    empty: boolean;
begin
  empty:=true;
  currentcol:=grid.col;
  for r:=0 to grid.rowcount -1 do begin
    if grid.cells[currentcol,r] <> '' then empty:=false;
  end;
  if not empty then but:=messagedlg(delcolnotempty,mtConfirmation,[mbyes,mbno],0);
  if ( empty ) or (but=mryes) then begin
    if currentcol < grid.colcount -1 then begin
      for c:=currentcol to grid.colcount -2 do begin
        for r:=0 to grid.rowcount -1 do begin
          grid.cells[c,r]:=grid.cells[c+1,r];
        end;
      end;
    end;
    grid.colcount:=grid.colcount -1;
  end;
  shadowupdate;
end;

procedure TForm1.SpeedButton10Click(Sender: TObject);
var quest: string;
begin
  speedbutton9.Click;
  quest:=inputbox('Time shift', timeadd,'00:30');
  if (quest <> '' )  and ( pos(':',quest) > 1 ) then  shifttimes(quest,grid.col);
  shadowupdate;
end;

procedure TForm1.SpeedButton11Click(Sender: TObject);
var lines: tstringlist;
    cell, line: String;
    cols, rows,c,r: integer;
begin
        lines:=tstringlist.create;
        cell:=getlastCell();
        rows:=strtoint(copy(cell,1,pos(':',cell)-1));
        cols:=strtoint(copy(cell,pos(':',cell)+1,length(cell)));
        for r:=0 to rows do begin
          line:='';
          for c:=0 to cols do begin
            line:=line+grid.cells[c,r]+';';
          end;
          line:=copy(line,1,length(line)-1);
          lines.add(line);
        end;
        fCES:=TCharEncStream.create;
        fCES.reset;
        fces.HasBOM:=true;
        fces.HaveType:=true;
        fces.UniStreamType:=ufUtf16le;
        fces.UTF8Text:=lines.text;
        fces.SaveToFile(UTF8ToSys(ttfilename));
        fces.Free;
        unsaved:=false;
end;

procedure TForm1.SpeedButton12Click(Sender: TObject);
begin
  shadow:=shadow -1;
  if shadow <1 then shadow:=1;
  restoreshadow(shadow);
  speedbutton13.enabled:=true;
end;

procedure TForm1.SpeedButton13Click(Sender: TObject);
begin
  shadow:=shadow+1;
  if shadow > shadowlist.count then shadow:=shadowlist.count;
  restoreshadow(shadow);
end;

procedure TForm1.SpeedButton14Click(Sender: TObject);
begin
  savedialog2.Filter:='*.zip|*.zip';
  savedialog2.Title:=savezip;
  if ( savedialog2.execute ) and ( savedialog2.filename <> '' ) then begin
    speedbutton11.Click;
    zipdata(savedialog2.filename);
  end;
end;

procedure TForm1.SpeedButton15Click(Sender: TObject);
var slist: tstringlist;
    z,s,mcols, col: integer;
    cols: tstringlist;
    cell: string;
begin
  cell:=getlastCell();
  col:=strtoint(copy(cell,pos(':',cell)+1,length(cell)));
  slist:=tstringlist.create;
  cols:=tstringlist.create;
  opendialog1.Title:=DLGImportTT;
  opendialog1.Filter:='Open Rails timetable|*.timetable-or;*.timetable_or';
  if (opendialog1.execute) and (opendialog1.FileName<>'') then begin
    fces:=tcharencStream.create;
    fces.reset;
    fces.loadfromfile(UTF8ToSys(opendialog1.filename));
    slist.text:=fces.utf8text;
    split(';',slist[0],cols);
    mcols:=0;
    mcols:=cols.count;
    if mcols+col+1 > grid.colcount then grid.colcount:=mcols+col+1;
    if slist.count +1 > grid.rowcount then grid.rowcount:=slist.count+1;
    for z:=0 to slist.count -1 do begin
      split(';',slist[z],cols);
      for s:= 1 to cols.count -1 do begin
        grid.cells[s+col,z]:=cols[s];
      end;
    end;
    fces.free;
    for s:=0 to grid.colcount -1 do begin
      autosizecol(grid, s);
    end;
    shadowupdate;
  end;
end;

procedure TForm1.SpeedButton16Click(Sender: TObject);
begin
  form4.show;
end;

end.

