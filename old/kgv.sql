/****** Object:  Table [dbo].[Aktenzeichen]    Script Date: 04.08.2025 09:50:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Aktenzeichen](
	[az_ID] [uniqueidentifier] NOT NULL,
	[az_Bezirk] [varchar](10) NULL,
	[az_Nummer] [int] NULL,
	[az_Jahr] [int] NULL,
 CONSTRAINT [PK_Aktenzeichen] PRIMARY KEY CLUSTERED 
(
	[az_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Antrag]    Script Date: 04.08.2025 09:50:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Antrag](
	[an_ID] [uniqueidentifier] NOT NULL,
	[an_Aktenzeichen] [varchar](20) NULL,
	[an_WartelistenNr32] [varchar](20) NULL,
	[an_WartelistenNr33] [varchar](20) NULL,
	[an_Anrede] [varchar](10) NULL,
	[an_Titel] [varchar](50) NULL,
	[an_Vorname] [varchar](50) NULL,
	[an_Nachname] [varchar](50) NULL,
	[an_Anrede2] [varchar](10) NULL,
	[an_Titel2] [varchar](50) NULL,
	[an_Vorname2] [varchar](50) NULL,
	[an_Nachname2] [varchar](50) NULL,
	[an_Briefanrede] [varchar](150) NULL,
	[an_Strasse] [varchar](50) NULL,
	[an_PLZ] [varchar](10) NULL,
	[an_Ort] [varchar](50) NULL,
	[an_Telefon] [varchar](50) NULL,
	[an_MobilTelefon] [varchar](50) NULL,
	[an_GeschTelefon] [varchar](50) NULL,
	[an_Bewerbungsdatum] [datetime] NULL,
	[an_Bestaetigungsdatum] [datetime] NULL,
	[an_AktuellesAngebot] [datetime] NULL,
	[an_Loeschdatum] [datetime] NULL,
	[an_Wunsch] [varchar](600) NULL,
	[an_Vermerk] [varchar](2000) NULL,
	[an_Aktiv] [char](1) NULL,
	[an_DeaktiviertAm] [datetime] NULL,
	[an_Geburtstag] [varchar](100) NULL,
	[an_Geburtstag2] [varchar](100) NULL,
	[an_MobilTelefon2] [varchar](50) NULL,
	[an_EMail] [varchar](100) NULL,
 CONSTRAINT [PK_Antrag] PRIMARY KEY CLUSTERED 
(
	[an_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Bezirk]    Script Date: 04.08.2025 09:50:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Bezirk](
	[bez_ID] [uniqueidentifier] NOT NULL,
	[bez_Name] [varchar](10) NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Bezirke_Katasterbezirke]    Script Date: 04.08.2025 09:50:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Bezirke_Katasterbezirke](
	[bez_Name] [varchar](10) NULL,
	[kat_Katasterbezirk] [varchar](10) NULL,
	[kat_KatasterbezirkName] [varchar](50) NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Eingangsnummer]    Script Date: 04.08.2025 09:50:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Eingangsnummer](
	[enr_ID] [uniqueidentifier] NOT NULL,
	[enr_Bezirk] [varchar](10) NULL,
	[enr_Nummer] [int] NULL,
	[enr_Jahr] [int] NULL,
 CONSTRAINT [PK_Eingangsnummer] PRIMARY KEY CLUSTERED 
(
	[enr_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Katasterbezirk]    Script Date: 04.08.2025 09:50:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Katasterbezirk](
	[kat_ID] [uniqueidentifier] NOT NULL,
	[kat_bez_ID] [uniqueidentifier] NULL,
	[kat_Katasterbezirk] [varchar](10) NULL,
	[kat_KatasterbezirkName] [varchar](50) NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Kennungen]    Script Date: 04.08.2025 09:50:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Kennungen](
	[Kenn_ID] [uniqueidentifier] NOT NULL,
	[Kenn_Name] [varchar](50) NULL,
	[Kenn_Domaene] [varchar](50) NULL,
	[Kenn_pers_ID] [uniqueidentifier] NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Mischenfelder]    Script Date: 04.08.2025 09:50:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Mischenfelder](
	[misch_ID] [uniqueidentifier] NOT NULL,
	[misch_Datenbankfeld] [varchar](50) NULL,
	[misch_Dokumentfeld] [varchar](50) NULL,
	[misch_Kommentar] [varchar](100) NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Personen]    Script Date: 04.08.2025 09:50:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Personen](
	[Pers_ID] [uniqueidentifier] NOT NULL,
	[Pers_Anrede] [varchar](10) NULL,
	[Pers_Vorname] [varchar](50) NULL,
	[Pers_Nachname] [varchar](50) NULL,
	[Pers_Nummer] [varchar](7) NULL,
	[Pers_Organisationseinheit] [varchar](10) NULL,
	[Pers_Zimmer] [varchar](10) NULL,
	[Pers_Telefon] [varchar](10) NULL,
	[Pers_FAX] [varchar](10) NULL,
	[Pers_Email] [varchar](50) NULL,
	[Pers_Diktatzeichen] [varchar](5) NULL,
	[Pers_Unterschrift] [varchar](50) NULL,
	[Pers_Dienstbezeichnung] [varchar](30) NULL,
	[Pers_Grp_ID] [uniqueidentifier] NULL,
	[Pers_istAdmin] [char](1) NULL,
	[Pers_darfAdministration] [char](1) NULL,
	[Pers_darfLeistungsgruppen] [char](1) NULL,
	[Pers_darfPrioUndSLA] [char](1) NULL,
	[Pers_darfKunden] [char](1) NULL,
	[Pers_Aktiv] [char](1) NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Verlauf]    Script Date: 04.08.2025 09:50:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Verlauf](
	[verl_ID] [uniqueidentifier] NOT NULL,
	[verl_An_ID] [uniqueidentifier] NULL,
	[verl_Art] [varchar](4) NULL,
	[verl_Datum] [datetime] NULL,
	[verl_Gemarkung] [varchar](50) NULL,
	[verl_Flur] [varchar](20) NULL,
	[verl_Parzelle] [varchar](20) NULL,
	[verl_Groesse] [varchar](20) NULL,
	[verl_Sachbearbeiter] [varchar](100) NULL,
	[verl_Hinweis] [varchar](100) NULL,
	[verl_Kommentar] [varchar](255) NULL,
 CONSTRAINT [PK_Verlauf] PRIMARY KEY CLUSTERED 
(
	[verl_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO