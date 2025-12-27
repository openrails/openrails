program timetableedit;

{$mode objfpc}{$H+}

uses
  {$IFDEF UNIX}{$IFDEF UseCThreads}
  cthreads,
  {$ENDIF}{$ENDIF}
  Interfaces, // this includes the LCL widgetset
  Forms, Unit1, about, stations, trains, dispose, sidings, tteobjects
  { you can add units after this };

{$R *.res}

begin
  RequireDerivedFormResource := True;
  Application.Initialize;
  Application.CreateForm(TForm1, Form1);
  Application.CreateForm(TForm2, Form2);
  Application.CreateForm(TForm5, Form5);
  Application.CreateForm(TForm3, Form3);
  Application.createForm(TForm6, Form6);
  Application.createForm(TForm4, Form4);
  Application.Run;
end.

