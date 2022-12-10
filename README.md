# hdm
Hdm stands for H-DOT Management and consists of a user client, windows service, updater, and proxy bypass. The remnants of the infamous "green atenna" at Hudson ISD

This project was developed back in 2009-2011 as the successor to the Windows XP version. Since we at the time were migrating away from Novel and XP, a complete rewrite was pretty much necessary. This version is for Windows 7 and supports using AMMT (Automatic Mouse Mover Thing) on the logon screen and desktop, bypassing session 0 isolation. 

This project was a part of a suite of projects including the Employee Console that was written in a LAMP stack (Linux/Apache/Mysql/PHP) and a Java app that served as a task server. It was later renamed myHISD and included integration with Google Apps - automatically creating accounts, calendars, contacts, and drive shares using Google's APIs. Employee Console served up the real time inventory, timesheets, AMMT, maps, and teacher evaluations. It supported most HTML5 and used jQuery with custom elements that supported drag, select all, etc. like you would be able to use in a file explorer - to manage the PC's. It required Firefox or Google Chrome 1.0*.

When Android 1.5 came out, we purchased two tablets and there was an Android app that was used to control a device with basic functions such as reboot or shut down. It worked by scanning a QR Code to access the device in the console and issuing it remote commands. This was a little bit before it's time and I was the only developer, so the project was abandoned. 

* Funny story. I used AMMT to install Google Chrome 1.0 on every PC in the district simultaneously. The reboot nearly caused a brown out, but if that wasn't enough, I forgot to uncheck default browser, so nothing worked that required IE. >.<
