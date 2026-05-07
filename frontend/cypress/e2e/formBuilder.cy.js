describe('template spec', () => {
  it('FirstTest', function() {
       cy.visit('http://localhost:5173/')
  });

  it('clientLogin', function() {
       cy.visit('http://localhost:5173/')
       
       cy.get('#svelte p.group-hover\\:text-sky-300').click();
       cy.get('#auth-email').click();
       cy.get('#svelte div.bg-slate-900\\/95').click();
       cy.get('#svelte button.w-full').click();
  });
})