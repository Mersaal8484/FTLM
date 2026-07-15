import React from 'react';
import { Navbar, Nav, Container, Button } from 'react-bootstrap';
import { NavLink } from 'react-router-dom';

function NavigationBar({ t, onToggleLanguage, isAuthenticated, onLogout }) {
  return (
    <Navbar expand="lg" className="navbar-custom shadow-sm">
      <Container>
        <Navbar.Brand as={NavLink} to="/">{t.brand}</Navbar.Brand>
        <Navbar.Toggle aria-controls="basic-navbar-nav" />
        <Navbar.Collapse id="basic-navbar-nav">
          <Nav className="me-auto">
            <Nav.Link as={NavLink} to="/">{t.dashboard}</Nav.Link>
            <Nav.Link as={NavLink} to="/fuel">{t.fuelInventory}</Nav.Link>
            <Nav.Link as={NavLink} to="/generators">{t.generators}</Nav.Link>
            <Nav.Link as={NavLink} to="/reports">{t.reports}</Nav.Link>
            {isAuthenticated && <Nav.Link as={NavLink} to="/users">{t.userManagement}</Nav.Link>}
          </Nav>
          <Nav className="align-items-center">
            <Button variant="outline-light" size="sm" className="btn-outline-toggle me-2" onClick={onToggleLanguage}>
              {t.switchLanguage}
            </Button>
            {isAuthenticated ? (
              <Button variant="outline-secondary" size="sm" onClick={onLogout}>{t.logout}</Button>
            ) : (
              <Button as={NavLink} to="/login" variant="outline-secondary" size="sm">
                {t.login}
              </Button>
            )}
          </Nav>
        </Navbar.Collapse>
      </Container>
    </Navbar>
  );
}

export default NavigationBar;
