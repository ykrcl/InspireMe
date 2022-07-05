# InspireMe


------------

### Installation:
- Create a postgresql user with given credentials in appsettings.json
- Create a postgresql database using the script given in Identity/Scrips/setup.txt
- Fill information for email smtp settings in appjson
- Load Project
- Run
*No Seed Data is Given*

------------
### Usage:
#### 1. Aim:
- Abilty to book meeting from professionals in the interested fields to get help developing and contemplating over ideas.

#### 2. Work  Procedure:
1.  Register (A thickbox is given to determine account type: Supervisor or Customer)
2. Login

##### For Cutomer:
- Book a Meeting
- Type fields interested in (It will autofill the box, if no autofill appears means that no supervisor exists in that field)
- Click the button "Book a meeting" in the table in the row of supervisor
- Select a date and hour in the appeared form which will also include price and availabilty information
- click Book
- When supervisor verifies booking the meeting will be available in current meetings.
- When the date and hour is available (or supervisor started meeting with a private conversation through email [Meeting might be missed and they decided to do it a later time])
> There will be a button to attend the meeting in the row in any meeting that the time has come or the supervisor currently in the meeting.

- click that button and start the chat

##### For Supervisor:
- Set Available Dates from the avaialable dates page by selecting a day (Monday, Tues...) and hours and setting a price for specified hours
- Check for meeting requests deny or verifiy them from requested meetings page
- When the date and hour is available (or supervisor started meeting with a private conversation through email [Meeting might be missed and they decided to do it a later time])

> There will be a button to attend the meeting in the row in any meetings that the time is come or passed and not started yet.

- click the button to start the chat.

### 3. Extra Specs:
- Send email  5 minute before the meeting
