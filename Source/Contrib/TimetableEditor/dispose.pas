unit dispose;

{$mode objfpc}{$H+}

interface

uses
  Classes, SysUtils, FileUtil, Forms, Controls, Graphics, Dialogs, StdCtrls,
  ExtCtrls, Buttons, maskedit, timetabledata, unit1;

type

  { TForm6 }

  TForm6 = class(TForm)
    BitBtn1: TBitBtn;
    BitBtn2: TBitBtn;
    BitBtn3: TBitBtn;
    BitBtn4: TBitBtn;
    BitBtn5: TBitBtn;
    BitBtn6: TBitBtn;
    CheckBox1: TCheckBox;
    check_f: TCheckBox;
    check_stat: TCheckBox;
    check_tr: TCheckBox;
    check_stop: TCheckBox;
    check_rr_t: TCheckBox;
    check_rrpos: TCheckBox;
    check_rr_p: TCheckBox;
    check_out_p: TCheckBox;
    check_in_p: TCheckBox;
    check_out_t: TCheckBox;
    check_in_t: TCheckBox;
    Combo_rr: TComboBox;
    Label_rr_p: TLabel;
    Label_out_p: TLabel;
    Label_in_p: TLabel;
    Label_f: TLabel;
    Label_tr: TLabel;
    ListBox1: TListBox;
    maskedit_rr_t: TMaskEdit;
    maskedit_out_t: TMaskEdit;
    maskedit_in_t: TMaskEdit;
    Panel1: TPanel;
    Panel2: TPanel;
    Panel3: TPanel;
    Panel4: TPanel;
    RadioButton1: TRadioButton;
    RadioButton2: TRadioButton;
    RadioButton3: TRadioButton;
    RadioButton4: TRadioButton;
    Speed_rr: TSpeedButton;
    Speed_out: TSpeedButton;
    Speed_in: TSpeedButton;
    Speed_f: TSpeedButton;
    Speed_tr: TSpeedButton;
    procedure BitBtn1Click(Sender: TObject);
    procedure BitBtn2Click(Sender: TObject);
    procedure BitBtn3Click(Sender: TObject);
    procedure BitBtn4Click(Sender: TObject);
    procedure BitBtn5Click(Sender: TObject);
    procedure BitBtn6Click(Sender: TObject);
    procedure CheckBox1Change(Sender: TObject);
    procedure check_fChange(Sender: TObject);
    procedure check_in_pChange(Sender: TObject);
    procedure check_in_tChange(Sender: TObject);
    procedure check_out_pChange(Sender: TObject);
    procedure check_out_tChange(Sender: TObject);
    procedure check_rrposChange(Sender: TObject);
    procedure check_rr_pChange(Sender: TObject);
    procedure check_rr_tChange(Sender: TObject);
    procedure check_trChange(Sender: TObject);
    procedure FormCreate(Sender: TObject);
    procedure FormShow(Sender: TObject);
    procedure ListBox1Click(Sender: TObject);
    procedure ListBox1DblClick(Sender: TObject);
    procedure Speed_fClick(Sender: TObject);
    procedure Speed_inClick(Sender: TObject);
    procedure Speed_outClick(Sender: TObject);
    procedure Speed_rrClick(Sender: TObject);
    procedure Speed_trClick(Sender: TObject);
  private
    { private declarations }
    procedure reset;
    procedure enable_disable;
    procedure disable_all;

  public
    { public declarations }
  end;

var
  Form6: TForm6;
  listtype: String;
  col: integer;

  resourceString
    irgendwas = 'irgendas';

implementation


{$R *.lfm}

{ TForm6 }

procedure tform6.reset;
begin
  radiobutton1.enabled:=true;
  radiobutton2.enabled:=true;
  radiobutton3.enabled:=true;
  radiobutton4.enabled:=true;
  radiobutton1.Checked:=true;
  panel2.visible:=false;
  panel3.visible:=false;
  label_out_p.caption:='';
  label_in_p.caption:='';
  label_rr_p.caption:='';
  label_f.caption:='';
  label_tr.caption:='';
  maskedit_out_t.Text:='';
  maskedit_in_t.text:='';
  maskedit_rr_t.Text:='';
  check_out_p.Checked:=false;
  check_out_t.checked:=false;
  check_in_p.checked:=false;
  check_in_t.checked:=false;
  check_rr_p.checked:=false;
  check_rr_t.checked:=false;
  check_rrpos.checked:=false;
  check_stop.Checked:=false;
  check_f.checked:=false;
  check_tr.Checked:=false;
  check_stat.Checked:=false;
  bitbtn1.enabled:=true;
end;

procedure tform6.enable_disable;
begin
  label_out_p.Enabled:=check_out_p.Checked;
  speed_out.Enabled:=check_out_p.checked;
  maskedit_out_t.Enabled:=check_out_t.checked;
  label_in_p.Enabled:=check_in_p.Checked;
  speed_in.Enabled:=check_in_p.checked;
  maskedit_in_t.Enabled:=check_in_t.checked;
  label_rr_p.Enabled:=check_rr_p.Checked;
  speed_rr.Enabled:=check_rr_p.checked;
  maskedit_rr_t.Enabled:=check_rr_t.checked;
  combo_rr.Enabled:=check_rrpos.Checked;
  label_f.Enabled:=check_f.Checked;
  speed_f.Enabled:=check_f.checked;
  label_tr.Enabled:=check_tr.Checked;
  speed_tr.Enabled:=check_tr.checked;
  if radiobutton1.checked then begin
     check_f.Checked:=true;
     check_f.Enabled:=false;
  end;
end;

procedure tform6.disable_all;
begin
  check_out_p.enabled:=false;
  check_out_t.enabled:=false;
  check_in_p.enabled:=false;
  check_in_t.enabled:=false;
  check_rr_p.enabled:=false;
  check_rr_t.enabled:=false;
  check_rrpos.enabled:=false;
  check_stop.enabled:=false;
  check_f.enabled:=false;
  check_tr.enabled:=false;
  check_stat.enabled:=false;
end;


function checktimes(time: string): String;
var i,n: integer;
begin
  n:=0;
  for i:=1 to length(time) do begin
     if time[i]=':' then inc(n);
  end;
  if n = 1 then time:=time+':00';
  if n = 0 then time:='';
  result:=time;
end;

procedure TForm6.FormCreate(Sender: TObject);
begin
  reset;
  enable_disable;
  panel2.Align:=alclient;
  panel3.align:=alclient;
  listtype:='';
end;

procedure TForm6.FormShow(Sender: TObject);
var line: String;
    li: tstringlist;
    i: integer;
begin
  reset;
  col:=form1.grid.col;
  if form1.grid.cells[form1.grid.Col,form1.grid.Row]<>'' then begin
     line:=form1.grid.cells[form1.grid.Col,form1.grid.Row];
     li:=tstringlist.create;
     split('/',line,li);
     bitbtn1.Enabled:=false;
     if pos('$forms',line) > 0 then begin
       radiobutton1.Checked:=true;
       for i:=length('$forms=') to length(line) do begin
          if (i = length(line) )or (line[i] = '/') then begin
              Label_f.caption:=trim(copy(line,length('$forms=')+1,i-length('$forms=')-1));
            break;
          end;
       end;
     end;
     if pos('$triggers', line) > 0 then radiobutton2.checked:=true;
     if pos('$static',line) > 0 then radiobutton3.checked:=true;
     if pos('$stable', line) > 0 then radiobutton4.checked:=true;
     disable_all;
     bitbtn1click(self);
     for i:= 1 to li.count -1 do begin
        if pos('out_path',li[i]) > 0 then begin
           check_out_p.checked:=true;
           label_out_p.Caption:=trim(copy(li[i],pos('=',li[i])+1,length(li[i])));
        end;
        if pos('out_time',li[i]) > 0 then begin
           check_out_t.checked:=true;
           maskedit_out_t.Text:=checktimes(trim(copy(li[i],pos('=',li[i])+1,length(li[i]))));
        end;
        if pos('in_path',li[i]) > 0 then begin
           check_in_p.checked:=true;
           label_in_p.Caption:=trim(copy(li[i],pos('=',li[i])+1,length(li[i])));
        end;
        if pos('in_time',li[i]) > 0 then begin
           check_in_t.checked:=true;
           maskedit_in_t.Text:=checktimes(trim(copy(li[i],pos('=',li[i])+1,length(li[i]))));
        end;
        if pos('runround',li[i]) > 0 then begin
           check_rr_p.checked:=true;
           label_rr_p.Caption:=trim(copy(li[i],pos('=',li[i])+1,length(li[i])));
        end;
        if pos('rrtime',li[i]) > 0 then begin
           check_rr_t.checked:=true;
           maskedit_rr_t.Text:=checktimes(trim(copy(li[i],pos('=',li[i])+1,length(li[i]))));
        end;
        if pos('rrpos',li[i]) > 0 then begin
           check_rrpos.Checked:=true;
           combo_rr.Text:=trim(copy(li[i],pos('=',li[i])+1,length(li[i])));
        end;
        if pos('setstop',li[i]) > 0 then begin
          check_stop.Checked:=true;
        end;
        if pos('forms',li[i]) > 0 then begin
           check_f.Checked:=true;
           label_f.Caption:=trim(copy(li[i],pos('=',li[i])+1,length(li[i])));
        end;
        if pos('triggers',li[i])> 0 then begin
           check_tr.checked:=true;
           label_tr.Caption:=trim(copy(li[i],pos('=',li[i])+1,length(li[i])));
        end;
        if pos('static',li[i]) > 0 then begin
          check_stat.Checked:=true;
        end;
     end;
     enable_disable;
  end;
end;

procedure TForm6.ListBox1Click(Sender: TObject);
begin

end;

procedure TForm6.ListBox1DblClick(Sender: TObject);
begin
  bitbtn3.Click;
end;



procedure TForm6.Speed_fClick(Sender: TObject);
begin
  panel2.visible:=false;
  panel3.visible:=true;
  listbox1.Items:=form1.gettrains(checkbox1.checked);
  if listbox1.items.count > 0 then listbox1.ItemIndex:=0;
  listtype:='forms';
end;

procedure TForm6.Speed_inClick(Sender: TObject);
begin
  panel2.visible:=false;
  panel3.visible:=true;
  listbox1.Items:=getpaths;
  if listbox1.items.count > 0 then listbox1.ItemIndex:=0;
  listtype:='in';
end;

procedure TForm6.Speed_outClick(Sender: TObject);
begin
  panel2.visible:=false;
  panel3.visible:=true;
  listbox1.Items:=getpaths;
  if listbox1.items.count > 0 then listbox1.ItemIndex:=0;
  listtype:='out';
end;

procedure TForm6.Speed_rrClick(Sender: TObject);
begin
  panel2.visible:=false;
  panel3.visible:=true;
  listbox1.Items:=getpaths;
  if listbox1.items.count > 0 then listbox1.ItemIndex:=0;
  listtype:='runround';
end;

procedure TForm6.Speed_trClick(Sender: TObject);
begin
  panel2.visible:=false;
  panel3.visible:=true;
  listbox1.Items:=form1.gettrains(checkbox1.checked);
  if listbox1.items.count > 0 then listbox1.ItemIndex:=0;
  listtype:='triggers';
end;

procedure TForm6.BitBtn1Click(Sender: TObject);
begin
  radiobutton1.enabled:=false;
  radiobutton2.enabled:=false;
  radiobutton3.enabled:=false;
  radiobutton4.enabled:=false;
  disable_all;
  panel2.Visible:=true;
  if radiobutton1.checked then begin       //forms
    Check_rr_p.enabled:=true;
    Check_rr_t.enabled:=true;
    check_f.Enabled:=true;
    check_stop.Enabled:=true;
  end;
  if radiobutton2.checked then begin       //triggers
    check_tr.Enabled:=true;
  end;
  if radiobutton3.Checked then begin       //static
     Check_out_p.enabled:=true;
     check_out_t.Enabled:=true;
  end;
  if radiobutton4.checked then begin       //stable
     check_out_p.Enabled:=true;
     check_out_t.enabled:=true;
     check_in_p.Enabled:=true;
     check_in_t.Enabled:=true;
     check_rr_p.enabled:=true;
     check_rr_t.enabled:=true;
     check_rrpos.Enabled:=true;
     check_f.Enabled:=true;
     check_tr.Enabled:=true;
     check_stat.Enabled:=true;
  end;
  enable_disable;
end;

procedure TForm6.BitBtn2Click(Sender: TObject);
begin
  reset;
end;

procedure TForm6.BitBtn3Click(Sender: TObject);
var tx: string;
begin
  tx:= '';
  if listbox1.Items.count > 0 then tx:=listbox1.Items[listbox1.ItemIndex];
  if listtype='out' then label_out_p.Caption:=tx;
  if listtype='in' then label_in_p.Caption:=tx;
  if listtype='runround' then label_rr_p.Caption:=tx;
  if listtype='forms' then label_f.Caption:=tx;
  if listtype='triggers' then label_tr.Caption:=tx;

  bitbtn4click(self);
end;

procedure TForm6.BitBtn4Click(Sender: TObject);
begin
  panel3.visible:=false;
  panel2.visible:=true;
  listtype:='';
end;

procedure TForm6.BitBtn5Click(Sender: TObject);
var disp: String;
begin
  disp:='';
  if radiobutton1.checked then disp:='$forms';
  if radiobutton2.checked then disp:='$triggers';
  if radiobutton3.checked then disp:='$static';
  if radiobutton4.checked then disp:='$stable';
  if radiobutton1.checked then disp:=disp+'='+label_f.caption;
  if (check_out_p.Checked) and (label_out_p.caption<>'') then disp:=disp+' /out_path='+label_out_p.caption;
  if (check_out_t.Checked) and (maskedit_out_t.text<>'') then disp:=disp+' /out_time='+maskedit_out_t.caption;
  if (check_in_p.Checked) and (label_in_p.caption<>'') then disp:=disp+' /in_path='+label_in_p.caption;
  if (check_in_t.Checked) and (maskedit_in_t.text<>'') then disp:=disp+' /in_time='+maskedit_in_t.caption;
  if (check_rr_p.Checked) and (label_rr_p.caption<>'') then disp:=disp+' /runround='+label_rr_p.caption;
  if (check_rr_t.Checked) and (maskedit_rr_t.text<>'') then disp:=disp+' /rrtime='+maskedit_rr_t.caption;
  if (check_rrpos.Checked) and (combo_rr.Caption<>'') then disp:=disp+' /rrpos='+combo_rr.caption;
  if (check_stop.Checked) then disp:=disp+' /setstop';
  if (check_f.Checked) and (label_f.caption<>'') and ( radiobutton1.checked=false )  then disp:=disp+' /forms='+label_f.caption;
  if (check_tr.Checked) and (label_tr.caption<>'') then disp:=disp+' /triggers='+label_tr.caption;
  if (check_stat.Checked) then disp:=disp+' /static';
  form1.grid.Cells[col,getrow('#dispose')]:=disp;
  form1.shadowupdate;
end;

procedure TForm6.BitBtn6Click(Sender: TObject);
begin
  form6.Close;
end;

procedure TForm6.CheckBox1Change(Sender: TObject);
begin
  listbox1.Items:=form1.gettrains(checkbox1.checked);
  if listbox1.items.count > 0 then listbox1.ItemIndex:=0;
end;

procedure TForm6.check_fChange(Sender: TObject);
begin
  enable_disable;
end;

procedure TForm6.check_in_pChange(Sender: TObject);
begin
  enable_disable;
end;

procedure TForm6.check_in_tChange(Sender: TObject);
begin
  enable_disable;
end;

procedure TForm6.check_out_pChange(Sender: TObject);
begin
  enable_disable;
end;

procedure TForm6.check_out_tChange(Sender: TObject);
begin
  enable_disable;
end;

procedure TForm6.check_rrposChange(Sender: TObject);
begin
  enable_disable;
end;

procedure TForm6.check_rr_pChange(Sender: TObject);
begin
  enable_disable;
end;

procedure TForm6.check_rr_tChange(Sender: TObject);
begin
  enable_disable;
end;

procedure TForm6.check_trChange(Sender: TObject);
begin
  enable_disable;
end;

end.

