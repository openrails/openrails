drop database if exists or_org_ccgijakema146330;
create database or_org_ccgijakema146330;
use or_org_ccgijakema146330;

CREATE TABLE tVisitor
  ( id  varchar(32) NOT NULL -- for an MD5 hash of datetime
  , PRIMARY KEY (id)
  ) ;

CREATE TABLE tWebpage
  ( url  varchar(255) NOT NULL -- e.g. "openrails.org/contact/"
  , updated_on timestamp NOT NULL
  , menu_path varchar(255) NOT NULL
  , PRIMARY KEY (url)
  ) ;
  
CREATE TABLE tVisit
  ( id  integer auto_increment NOT NULL
  , made_by varchar(32) NOT NULL
  , made_to varchar(255) NOT NULL
  , visited_on timestamp NOT NULL
  , PRIMARY KEY (id)
  , FOREIGN KEY (made_by) REFERENCES tVisitor (id)
  , FOREIGN KEY (made_to) REFERENCES tWebpage (url)
  ) ;
  
CREATE TABLE tVisitor_Attribute
  ( id  integer auto_increment NOT NULL
  , used_by varchar(32) NOT NULL
  , of_type varchar(255)  -- e.g. "IP"
  , of_value varchar(255) -- e.g. "192.168.0.1"
  , PRIMARY KEY (id)
  , FOREIGN KEY (used_by) REFERENCES tVisitor (id)
  ) ;
  