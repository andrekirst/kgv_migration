# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a database migration project for a KGV (Kleingartenverein - German allotment garden association) system. The repository contains SQL database schema and data migration scripts for transitioning from a legacy system.

## Database Schema

The project centers around a SQL Server database with 10 main entities:

- **Aktenzeichen** - File reference numbers with district, number, and year
- **Antrag** - Applications with personal data, contact info, and application status
- **Bezirk** - Districts/regions
- **Bezirke_Katasterbezirke** - Junction table linking districts to cadastral areas
- **Eingangsnummer** - Entry numbers
- **Katasterbezirk** - Cadastral districts
- **Kennungen** - Identifiers/codes
- **Mischenfelder** - Mixed/miscellaneous fields
- **Personen** - Person records
- **Verlauf** - History/timeline records

## Key Files

- `old/kgv.sql` - Complete database schema export (182 lines) containing CREATE TABLE statements for all entities
- `README.md` - Basic project description

## Development Notes

This is a data migration project focused on SQL database structures. There are no build tools, test frameworks, or application code present - only database migration scripts and schema definitions.

The schema appears to handle German administrative data with fields for addresses, phone numbers, application dates, and various administrative identifiers typical of municipal or association management systems.