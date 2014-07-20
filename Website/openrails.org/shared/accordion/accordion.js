// Script for simple accordion
// Based on http://www.jacklmoore.com/notes/jquery-accordion-tutorial/
    $(document).ready(function(){
      $('.accordion_head').click(function(e){
        e.preventDefault();
        $(this).closest('li').find('.accordion_body').slideToggle();
      });
    });
