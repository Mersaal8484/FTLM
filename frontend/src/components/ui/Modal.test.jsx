import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, test, expect, vi } from 'vitest';
import Modal from './Modal';

describe('UI Modal Component', () => {
  test('does not render when open is false', () => {
    const { container } = render(
      <Modal open={false} onClose={vi.fn()} title="Test Modal">
        Modal Content
      </Modal>
    );
    expect(container).toBeEmptyDOMElement();
  });

  test('renders title, content and footer actions when open is true', () => {
    const handleClose = vi.fn();
    render(
      <Modal
        open={true}
        onClose={handleClose}
        title="Edit Generator"
        footer={<button data-testid="save-btn">Save Changes</button>}
      >
        <p>Form body goes here</p>
      </Modal>
    );

    expect(screen.getByText('Edit Generator')).toBeInTheDocument();
    expect(screen.getByText('Form body goes here')).toBeInTheDocument();
    expect(screen.getByTestId('save-btn')).toBeInTheDocument();
  });

  test('calls onClose when close button is clicked', () => {
    const handleClose = vi.fn();
    render(
      <Modal open={true} onClose={handleClose} title="Test Dialog">
        Content
      </Modal>
    );

    const closeBtn = screen.getByRole('button', { name: /Close modal/i });
    fireEvent.click(closeBtn);
    expect(handleClose).toHaveBeenCalledTimes(1);
  });
});
