// overrides.js

// Override getPositionDetails function
function overrideGetPositionDetails() {
    const originalGetPositionDetails = window.getPositionDetails;
    
    window.getPositionDetails = function(e, t) {
      // Modify the e object to set skipValidation to true
      e.skipValidation = true;
      
      // Call the original getPositionDetails function with modified e object
      return originalGetPositionDetails.call(this, e, t);
    }
  }
  
  // Execute the override function
  overrideGetPositionDetails();
  